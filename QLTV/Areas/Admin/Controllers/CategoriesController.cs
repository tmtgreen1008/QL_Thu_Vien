using LibraryManagement.Areas.Admin.ViewModels;
using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _db;

    public CategoriesController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? search)
    {
        var query = _db.Categories.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search));

        var rows = await query
            .OrderBy(c => c.Name)
            .Select(c => new NamedItemRow
            {
                Id = c.Id,
                Name = c.Name,
                Extra = c.Description,
                BookCount = c.Books.Count
            })
            .ToListAsync();

        ViewData["Search"] = search;
        return View(rows);
    }

    public IActionResult Create() => View(new Category());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Description")] Category category)
    {
        if (await _db.Categories.AnyAsync(c => c.Name == category.Name))
            ModelState.AddModelError(nameof(Category.Name), "Thể loại này đã tồn tại.");

        if (!ModelState.IsValid) return View(category);

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm thể loại \"{category.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound();
        return View(category);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Category category)
    {
        if (id != category.Id) return NotFound();

        if (await _db.Categories.AnyAsync(c => c.Name == category.Name && c.Id != id))
            ModelState.AddModelError(nameof(Category.Name), "Thể loại này đã tồn tại.");

        if (!ModelState.IsValid) return View(category);

        var existing = await _db.Categories.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Name = category.Name;
        existing.Description = category.Description;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã cập nhật thể loại.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound();

        if (await _db.Books.AnyAsync(b => b.CategoryId == id))
        {
            TempData["Error"] = $"Không thể xóa \"{category.Name}\" vì vẫn còn sách thuộc thể loại này.";
            return RedirectToAction(nameof(Index));
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa thể loại \"{category.Name}\".";
        return RedirectToAction(nameof(Index));
    }
}
