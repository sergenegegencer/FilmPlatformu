using FilmPlatformu.Data;
using FilmPlatformu.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Panel(string? tab, string? filterUsername, string? filterFilmTitle)
    {
        var allReviewsQuery = _context.Reviews.Include(r => r.Film).AsQueryable();

        if (!string.IsNullOrEmpty(filterUsername))
            allReviewsQuery = allReviewsQuery.Where(r => r.UserName!.Contains(filterUsername));

        if (!string.IsNullOrEmpty(filterFilmTitle))
            allReviewsQuery = allReviewsQuery.Where(r => r.Film != null && r.Film.Title!.Contains(filterFilmTitle));

        var allReviews = await allReviewsQuery.ToListAsync();
        var popularFilms = await _tmdb.GetPopularMoviesAsync();
        var userCount = await _context.Users.CountAsync();

        var vm = new AdminPanelViewModel
        {
            AllReviews = allReviews,
            PopularFilms = popularFilms,
            TotalUsers = userCount,
            TotalReviews = allReviews.Count,
            FilterUsername = filterUsername,
            FilterFilmTitle = filterFilmTitle,
            SelectedTab = tab ?? "summary" // ✅ BURASI ÖNEMLİ
        };

        return View(vm);
    }

    private readonly ApplicationDbContext _context;
    private readonly TMDBService _tmdb;

    public AdminController(ApplicationDbContext context, TMDBService tmdb)
    {
        _context = context;
        _tmdb = tmdb;
    }

    [HttpPost]
    public async Task<IActionResult> CleanDuplicateReviews()
    {
        var allReviews = await _context.Reviews
            .Where(r => !string.IsNullOrEmpty(r.UserId))
            .ToListAsync(); // 🧠 veritabanından tüm verileri çekiyoruz

        var redundantReviews = allReviews
            .GroupBy(r => new { r.UserId, r.FilmId })
            .SelectMany(g => g.OrderByDescending(r => r.Date).Skip(1)) // en güncel olanı bırak
            .ToList();

        int deletedCount = redundantReviews.Count;

        _context.Reviews.RemoveRange(redundantReviews);
        await _context.SaveChangesAsync();

        TempData["CleanResult"] = $"{deletedCount} yorum başarıyla silindi.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAllReviews()
    {
        var allReviews = await _context.Reviews.ToListAsync();

        _context.Reviews.RemoveRange(allReviews);
        await _context.SaveChangesAsync();

        TempData["CleanResult"] = $"{allReviews.Count} yorum tamamen silindi.";
        return RedirectToAction("Index", "Home");
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AllReviews()
    {
        var reviews = await _context.Reviews
            .Include(r => r.Film)
            .OrderByDescending(r => r.Date)
            .ToListAsync();

        return View(reviews);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review != null)
        {
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"\"{review.Film?.Title}\" filmine ait yorum silindi.";
        }

        return RedirectToAction("AllReviews");
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteReviewFromPanel(int id)
    {
        var review = await _context.Reviews
            .Include(r => r.Film)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review != null)
        {
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            TempData["ReviewDeleted"] = $"“{review.Film?.Title ?? "Film"}” filmine yapılan yorum silindi.";
        }

        return RedirectToAction("Panel", new { tab = "reviews" });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SearchTmdb(
    string? query, string? filterGenre, string? filterYear, string? filterLanguage, int page = 1)
    {
        SearchResult result;

        if (string.IsNullOrWhiteSpace(query))
        {
            var allFilms = await _tmdb.GetPopularMoviesAsync();
            var filtered = allFilms;

            if (!string.IsNullOrWhiteSpace(filterGenre))
                filtered = filtered.Where(f => f.Genres?.Contains(filterGenre, StringComparison.OrdinalIgnoreCase) == true).ToList();

            if (!string.IsNullOrWhiteSpace(filterYear))
                filtered = filtered.Where(f => f.ReleaseDate != null && f.ReleaseDate.StartsWith(filterYear)).ToList();

            if (!string.IsNullOrWhiteSpace(filterLanguage))
                filtered = filtered.Where(f => f.OriginalLanguage?.Equals(filterLanguage, StringComparison.OrdinalIgnoreCase) == true).ToList();

            result = new SearchResult
            {
                Films = filtered.Skip((page - 1) * 20).Take(20).ToList(),
                TotalPages = (int)Math.Ceiling(filtered.Count / 20.0)
            };
        }
        else
        {
            result = await _tmdb.SearchMoviesAsync(query, page);
        }

        var vm = new AdminPanelViewModel
        {
            PopularFilms = result.Films,
            TotalPages = result.TotalPages,
            CurrentPage = page,
            FilterGenre = filterGenre,
            FilterYear = filterYear,
            FilterLanguage = filterLanguage,
            Query = query,
            SelectedTab = "tmdb"
        };

        return View("Panel", vm);
    }


}
