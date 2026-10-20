using LibraryManagement.Areas.Admin.ViewModels;
using LibraryManagement.Data;
using LibraryManagement.Models.Enums;
using LibraryManagement.Services;
using LibraryManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers;

// Tra cứu sách công khai. Lần tải đầu do server dựng HTML; sau đó catalog.js gọi REST API để lọc tức thì.
public class CatalogController : Controller
{
    private const int PageSize = 12;
    private readonly ApplicationDbContext _db;

    public CatalogController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q, int? categoryId, bool availableOnly = false,
        string? sort = null, int page = 1)
    {
        sort = sort == "new" ? "new" : "title";
        var query = _db.Books.AsNoTracking().InStock().Search(q, categoryId, availableOnly, sort);

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = await query.Skip((page - 1) * PageSize).Take(PageSize)
            .Select(BookQueries.ToCard).ToListAsync();

        var cats = await _db.Categories.AsNoTracking().OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name }).ToListAsync();

        return View(new CatalogIndexVm
        {
            Result = new PagedResult<BookCardVm> { Items = items, Page = page, PageSize = PageSize, TotalItems = total },
            Q = q?.Trim(),
            CategoryId = categoryId,
            AvailableOnly = availableOnly,
            Sort = sort,
            Categories = cats.Select(c => new SelectListItem(c.Name, c.Id.ToString(), c.Id == categoryId)).ToList()
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var book = await _db.Books.AsNoTracking()
            .Include(b => b.Author).Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id && b.IsActive);
        if (book == null) return NotFound();

        var copies = await _db.BookCopies.AsNoTracking().Where(c => c.BookId == id)
            .Select(c => new { c.Status, c.ReadOnSiteOnly }).ToListAsync();

        var related = await _db.Books.AsNoTracking().InStock()
            .Where(b => b.Id != id && b.CategoryId == book.CategoryId)
            .OrderBy(b => b.Title).Take(4)
            .Select(BookQueries.ToCard).ToListAsync();

        RecentCookie.Push(HttpContext, id);

        return View(new CatalogDetailsVm
        {
            Book = book,
            Borrowable = copies.Count(c => c.Status == CopyStatus.Available && !c.ReadOnSiteOnly),
            ReadOnSite = copies.Count(c => c.Status == CopyStatus.Available && c.ReadOnSiteOnly),
            TotalCopies = copies.Count(c => c.Status != CopyStatus.Disposed),
            InWishlist = WishlistSession.Get(HttpContext).Contains(id),
            Related = related
        });
    }
}
