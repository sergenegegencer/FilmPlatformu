﻿using FilmPlatformu.Data;
using FilmPlatformu.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FilmPlatformu.Controllers
{
    public class FilmController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly TMDBService _tmdb;
        private readonly UserManager<IdentityUser> _userManager;

        public FilmController(ApplicationDbContext context, TMDBService tmdb, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _tmdb = tmdb;
            _userManager = userManager;
        }

        public IActionResult Detail(int id)
        {
            var film = _context.Films
                .Include(f => f.Reviews)
                .FirstOrDefault(f => f.Id == id);
            return View(film);
        }

        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return View(new List<Film>());

            var results = await _tmdb.SearchMoviesAsync(query);
            return View(results);
        }

        public async Task<IActionResult> DetailByTmdb(int tmdbId)
        {
            var userId = _userManager.GetUserId(User);

            var film = await _context.Films
                .Include(f => f.Reviews)
                .FirstOrDefaultAsync(f => f.TMDBId == tmdbId);

            if (film == null)
            {
                var filmFromApi = await _tmdb.GetFilmByIdAsync(tmdbId);
                film = filmFromApi;
            }
            else if (string.IsNullOrEmpty(film.Overview) || string.IsNullOrEmpty(film.ReleaseDate))
            {
                var filmFromApi = await _tmdb.GetFilmByIdAsync(tmdbId);

                film.Overview = filmFromApi.Overview;
                film.ReleaseDate = filmFromApi.ReleaseDate;
                film.Runtime = filmFromApi.Runtime;
                film.OriginalLanguage = filmFromApi.OriginalLanguage;
                film.Genres = filmFromApi.Genres;

                _context.Films.Update(film);
                await _context.SaveChangesAsync();
            }

            bool isInWatchlist = false;
            if (userId != null && film.Id != 0)
            {
                isInWatchlist = await _context.Watchlists
                    .AnyAsync(w => w.UserId == userId && w.FilmId == film.Id);
            }

            var vm = new FilmDetailViewModel
            {
                Film = film,
                IsInWatchlist = isInWatchlist
            };

            return View("TmdbDetail", vm);
        }


        [HttpPost]
        public async Task<IActionResult> AddReview(int filmId, string comment, int rating)
        {
            var userId = _userManager.GetUserId(User);
            var userName = User.Identity?.Name ?? "Anonim";

            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.FilmId == filmId && r.UserId == userId);

            if (existingReview != null)
            {
                existingReview.Comment = comment;
                existingReview.Rating = rating;
                existingReview.Date = DateTime.Now;
            }
            else
            {
                var review = new Review
                {
                    FilmId = filmId,
                    UserId = userId!,
                    UserName = userName!,
                    Rating = rating,
                    Comment = comment,
                    Date = DateTime.Now
                };

                _context.Reviews.Add(review);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Detail", "Film", new { id = filmId });
        }

        [HttpPost]
        public async Task<IActionResult> SaveAndReview(int tmdbId, string comment, int rating)
        {
            var userId = _userManager.GetUserId(User);
            var userName = User.Identity?.Name ?? "Anonim";

            // Film DB'de var mı kontrol et
            var film = await _context.Films.FirstOrDefaultAsync(f => f.TMDBId == tmdbId);
            if (film == null)
            {
                var filmFromApi = await _tmdb.GetFilmByIdAsync(tmdbId);
                film = new Film
                {
                    TMDBId = tmdbId,
                    Title = filmFromApi.Title,
                    PosterUrl = filmFromApi.PosterUrl,
                    Overview = filmFromApi.Overview,
                    Runtime = filmFromApi.Runtime,
                    Genres = filmFromApi.Genres,
                    ReleaseDate = filmFromApi.ReleaseDate,
                    OriginalLanguage = filmFromApi.OriginalLanguage
                };

                _context.Films.Add(film);
                await _context.SaveChangesAsync();
            }

            // ✅ Artık UserId ile kontrol yapılıyor
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.FilmId == film.Id && r.UserId == userId);

            if (existingReview != null)
            {
                existingReview.Comment = comment;
                existingReview.Rating = rating;
                existingReview.Date = DateTime.Now;
            }
            else
            {
                var review = new Review
                {
                    FilmId = film.Id,
                    UserId = userId!,
                    UserName = userName!,
                    Rating = rating,
                    Comment = comment,
                    Date = DateTime.Now
                };

                _context.Reviews.Add(review);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("DetailByTmdb", "Film", new { tmdbId = film.TMDBId });
        }

        [HttpPost]
        public async Task<IActionResult> AddToWatchlist(int tmdbId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Unauthorized();

            var film = await _context.Films.FirstOrDefaultAsync(f => f.TMDBId == tmdbId);
            if (film == null)
            {
                var filmFromApi = await _tmdb.GetFilmByIdAsync(tmdbId);
                film = new Film
                {
                    TMDBId = tmdbId,
                    Title = filmFromApi.Title,
                    PosterUrl = filmFromApi.PosterUrl
                };
                _context.Films.Add(film);
                await _context.SaveChangesAsync();
            }

            var alreadyExists = await _context.Watchlists
                .AnyAsync(w => w.UserId == userId && w.FilmId == film.Id);

            if (!alreadyExists)
            {
                var watchlist = new Watchlist
                {
                    FilmId = film.Id,
                    UserId = userId
                };
                _context.Watchlists.Add(watchlist);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("DetailByTmdb", new { tmdbId = film.TMDBId });
        }

        public async Task<IActionResult> MyWatchlist()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var watchlist = await _context.Watchlists
                .Include(w => w.Film)
                .Where(w => w.UserId == userId)
                .Select(w => w.Film!)
                .ToListAsync();

            return View("Watchlist", watchlist);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromWatchlist(int tmdbId, string returnTo = "MyWatchlist")
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
                return Unauthorized();

            var film = await _context.Films.FirstOrDefaultAsync(f => f.TMDBId == tmdbId);
            if (film == null)
                return NotFound();

            var existing = await _context.Watchlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.FilmId == film.Id);

            if (existing != null)
            {
                _context.Watchlists.Remove(existing);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"🎞 \"{film.Title}\" watchlist’ten kaldırıldı.";
            }

            // 🔄 Hangi sayfaya dönüleceğini kontrol et
            return returnTo switch
            {
                "DetailByTmdb" => RedirectToAction("DetailByTmdb", new { tmdbId }),
                _ => RedirectToAction("MyWatchlist")
            };
        }


    }

}
