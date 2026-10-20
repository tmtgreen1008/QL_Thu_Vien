using LibraryManagement.Areas.Admin.ViewModels;
using LibraryManagement.Data;
using LibraryManagement.Models.Enums;
using LibraryManagement.Services;
using LibraryManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers;

// Trang chủ công khai (không cần đăng nhập)
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var inStock = _db.Books.AsNoTracking()
            .Where(b => b.IsActive && b.Copies.Any(c => c.Status != CopyStatus.Disposed));

        var latest = await inStock.OrderByDescending(b => b.Id).Take(8)
            .Select(BookQueries.ToCard).ToListAsync();

        // Sách xem gần đây: đọc từ cookie
        var recent = new List<BookCardVm>();
        var recentIds = RecentCookie.Get(Request);
        if (recentIds.Count > 0)
        {
            var found = await inStock.Where(b => recentIds.Contains(b.Id))
                .Select(BookQueries.ToCard).ToListAsync();
            recent = recentIds.Select(id => found.FirstOrDefault(f => f.Id == id))
                .Where(f => f != null).Select(f => f!).ToList();
        }

        var categories = await _db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryCardVm
            {
                Id = c.Id,
                Name = c.Name,
                Titles = c.Books.Count(b => b.IsActive)
            })
            .ToListAsync();

        return View(new HomeVm
        {
            Titles = await inStock.CountAsync(),
            Copies = await _db.BookCopies.CountAsync(c => c.Status != CopyStatus.Disposed),
            Latest = latest,
            Recent = recent,
            Categories = categories.Where(c => c.Titles > 0).ToList()
        });
    }

    public IActionResult Error() => View();
}
