using FilmPlatformu.Data;
using FilmPlatformu.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize]
public class UserController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly TMDBService _tmdb;

    public UserController(ApplicationDbContext context, UserManager<IdentityUser> userManager, TMDBService tmdb)
    {
        _context = context;
        _userManager = userManager;
        _tmdb = tmdb;
    }

    [Authorize(Roles = "User")]
    public async Task<IActionResult> Panel(string? query, string? filterGenre, string? filterYear, string? filterLanguage, int page = 1)
    {
        SearchResult result;

        if (string.IsNullOrWhiteSpace(query))
        {
            // Popüler filmler: ilk 100 film içinden sayfalama
            var allFilms = await _tmdb.GetPopularMoviesAsync(); // 5 sayfa = 100 film
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
            // Arama yapılmışsa: SearchMoviesAsync yeni haliyle kullanılır
            result = await _tmdb.SearchMoviesAsync(query, page);
        }

        var vm = new UserPanelViewModel
        {
            Films = result.Films,
            Query = query,
            FilterGenre = filterGenre,
            FilterYear = filterYear,
            FilterLanguage = filterLanguage,
            CurrentPage = page,
            TotalPages = result.TotalPages
        };

        return View(vm);
    }


    // [Authorize(Roles = "User")]
    public async Task<IActionResult> MyReviews()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null) return Unauthorized();

        var reviews = await _context.Reviews
            .Include(r => r.Film)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.Date)
            .ToListAsync();

        return View(reviews);
    }

    [Authorize(Roles = "User")]
    public async Task<IActionResult> SearchTmdb(string? query, string? filterGenre, string? filterYear, string? filterLanguage)
    {
        List<Film> results;

        if (string.IsNullOrWhiteSpace(query))
        {
            results = await _tmdb.GetPopularMoviesAsync();
        }
        else
        {
            var searchResult = await _tmdb.SearchMoviesAsync(query);
            results = searchResult.Films;
        }

        if (!string.IsNullOrWhiteSpace(filterGenre))
        {
            results = results.Where(f =>
                !string.IsNullOrEmpty(f.Genres) &&
                f.Genres.Contains(filterGenre, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        if (!string.IsNullOrWhiteSpace(filterYear))
        {
            results = results.Where(f =>
                !string.IsNullOrEmpty(f.ReleaseDate) &&
                f.ReleaseDate.StartsWith(filterYear)
            ).ToList();
        }

        if (!string.IsNullOrWhiteSpace(filterLanguage))
        {
            results = results.Where(f =>
                !string.IsNullOrEmpty(f.OriginalLanguage) &&
                f.OriginalLanguage.Equals(filterLanguage, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        var vm = new UserPanelViewModel
        {
            Films = results,
            FilterGenre = filterGenre,
            FilterYear = filterYear,
            FilterLanguage = filterLanguage,
            Query = query
        };

        return View("SearchTmdb", vm);
    }

}

