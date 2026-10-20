using LibraryManagement.Areas.Admin.ViewModels;
using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class AuthorsController : Controller
{
    private readonly ApplicationDbContext _db;

    public AuthorsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? search)
    {
        var query = _db.Authors.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.Name.Contains(search));

        var rows = await query
            .OrderBy(a => a.Name)
            .Select(a => new NamedItemRow
            {
                Id = a.Id,
                Name = a.Name,
                Extra = a.Bio,
                BookCount = a.Books.Count
            })
            .ToListAsync();

        ViewData["Search"] = search;
        return View(rows);
    }

    public IActionResult Create() => View(new Author());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Bio")] Author author)
    {
        if (!ModelState.IsValid) return View(author);

        _db.Authors.Add(author);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm tác giả \"{author.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var author = await _db.Authors.FindAsync(id);
        if (author == null) return NotFound();
        return View(author);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Bio")] Author author)
    {
        if (id != author.Id) return NotFound();
        if (!ModelState.IsValid) return View(author);

        var existing = await _db.Authors.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Name = author.Name;
        existing.Bio = author.Bio;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã cập nhật tác giả.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var author = await _db.Authors.FindAsync(id);
        if (author == null) return NotFound();

        if (await _db.Books.AnyAsync(b => b.AuthorId == id))
        {
            TempData["Error"] = $"Không thể xóa \"{author.Name}\" vì vẫn còn sách của tác giả này.";
            return RedirectToAction(nameof(Index));
        }

        _db.Authors.Remove(author);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa tác giả \"{author.Name}\".";
        return RedirectToAction(nameof(Index));
    }
}
