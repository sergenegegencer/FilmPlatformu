using FilmPlatformu.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    public IActionResult Panel()
    {
        return View();
    }

    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
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
    public async Task<IActionResult> DeleteAllReviews()
    {
        var allReviews = await _context.Reviews.ToListAsync();

        _context.Reviews.RemoveRange(allReviews);
        await _context.SaveChangesAsync();

        TempData["CleanResult"] = $"{allReviews.Count} yorum tamamen silindi.";
        return RedirectToAction("Index", "Home");
    }
}
