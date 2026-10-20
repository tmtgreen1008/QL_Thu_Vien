using LibraryManagement.Data;
using LibraryManagement.Services;
using LibraryManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers;

// Giỏ sách quan tâm lưu trong SESSION
public class WishlistController : Controller
{
    private readonly ApplicationDbContext _db;

    private readonly Loc _t;

    public WishlistController(ApplicationDbContext db, Loc t)
    {
        _db = db;
        _t = t;
    }

    public async Task<IActionResult> Index()
    {
        var ids = WishlistSession.Get(HttpContext);
        var found = ids.Count == 0
            ? new List<BookCardVm>()
            : await _db.Books.AsNoTracking()
                .Where(b => b.IsActive && ids.Contains(b.Id))
                .Select(BookQueries.ToCard).ToListAsync();

        // Giữ đúng thứ tự người dùng đã thêm
        var items = ids.Select(id => found.FirstOrDefault(f => f.Id == id))
            .Where(f => f != null).Select(f => f!).ToList();
        return View(new WishlistVm { Items = items });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int bookId, string? returnUrl)
    {
        if (!await _db.Books.AnyAsync(b => b.Id == bookId && b.IsActive)) return NotFound();

        var ids = WishlistSession.Get(HttpContext);
        if (ids.Contains(bookId))
            TempData["Success"] = _t["wish.exists"];
        else if (ids.Count >= WishlistSession.MaxItems)
            TempData["Error"] = _t["wish.full", WishlistSession.MaxItems];
        else
        {
            ids.Add(bookId);
            WishlistSession.Save(HttpContext, ids);
            TempData["Success"] = _t["wish.added"];
        }
        return Back(returnUrl);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Remove(int bookId, string? returnUrl)
    {
        var ids = WishlistSession.Get(HttpContext);
        if (ids.Remove(bookId)) WishlistSession.Save(HttpContext, ids);
        TempData["Success"] = _t["wish.removed"];
        return Back(returnUrl);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        WishlistSession.Save(HttpContext, new List<int>());
        TempData["Success"] = _t["wish.cleared"];
        return RedirectToAction(nameof(Index));
    }

    private IActionResult Back(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
}
