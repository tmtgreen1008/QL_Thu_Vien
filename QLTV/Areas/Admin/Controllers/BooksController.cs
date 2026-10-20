using LibraryManagement.Areas.Admin.Services;
using LibraryManagement.Areas.Admin.ViewModels;
using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class BooksController : Controller
{
    private const int PageSize = 10;
    private const long MaxCoverBytes = 2 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public BooksController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // ---------- Danh sách: tìm kiếm, lọc thể loại, phân trang ----------
    public async Task<IActionResult> Index(string? search, int? categoryId, int page = 1)
    {
        var query = _db.Books.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(b => b.Title.Contains(search)
                                     || b.ISBN.Contains(search)
                                     || b.Author!.Name.Contains(search));
        }

        if (categoryId.HasValue)
            query = query.Where(b => b.CategoryId == categoryId.Value);

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = await query
            .OrderBy(b => b.Title)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(b => new BookRow
            {
                Id = b.Id,
                Title = b.Title,
                ISBN = b.ISBN,
                AuthorName = b.Author!.Name,
                CategoryName = b.Category!.Name,
                RentalFee = b.RentalFee,
                IsActive = b.IsActive,
                CoverImagePath = b.CoverImagePath,
                TotalCopies = b.Copies.Count(c => c.Status != CopyStatus.Disposed),
                AvailableCopies = b.Copies.Count(c => c.Status == CopyStatus.Available)
            })
            .ToListAsync();

        var vm = new BookIndexVm
        {
            Result = new PagedResult<BookRow>
            {
                Items = items,
                Page = page,
                PageSize = PageSize,
                TotalItems = total
            },
            Search = search,
            CategoryId = categoryId,
            Categories = await CategoryItemsAsync(categoryId)
        };
        return View(vm);
    }

    // ---------- Chi tiết sách + danh sách bản sao ----------
    public async Task<IActionResult> Details(int id)
    {
        var book = await _db.Books.AsNoTracking()
            .Include(b => b.Author)
            .Include(b => b.Category)
            .Include(b => b.Copies)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (book == null) return NotFound();
        return View(book);
    }

    // ---------- Thêm ----------
    public async Task<IActionResult> Create()
    {
        var vm = new BookFormVm();
        await FillListsAsync(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookFormVm vm)
    {
        vm.ISBN = vm.ISBN?.Trim() ?? string.Empty;

        if (await _db.Books.AnyAsync(b => b.ISBN == vm.ISBN))
            ModelState.AddModelError(nameof(vm.ISBN), "ISBN này đã tồn tại.");

        ValidateCover(vm.CoverFile);
        await ValidateReferencesAsync(vm);

        if (!ModelState.IsValid)
        {
            await FillListsAsync(vm);
            return View(vm);
        }

        string? coverPath = null;
        if (vm.CoverFile is { Length: > 0 })
            coverPath = await SaveCoverAsync(vm.CoverFile);

        var book = new Book
        {
            Title = vm.Title.Trim(),
            ISBN = vm.ISBN,
            Description = vm.Description,
            Publisher = vm.Publisher,
            PublishYear = vm.PublishYear,
            PageCount = vm.PageCount,
            CoverImagePath = coverPath,
            RentalFee = vm.RentalFee,
            ReplacementPrice = vm.ReplacementPrice,
            AuthorId = vm.AuthorId,
            CategoryId = vm.CategoryId,
            IsActive = vm.IsActive
        };

        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            _db.Books.Add(book);
            await _db.SaveChangesAsync();

            if (vm.InitialCopies > 0)
            {
                var barcodes = await BarcodeGenerator.NextAsync(_db, book.Id, vm.InitialCopies);
                foreach (var code in barcodes)
                {
                    _db.BookCopies.Add(new BookCopy
                    {
                        BookId = book.Id,
                        Barcode = code,
                        ShelfLocation = vm.ShelfLocation,
                        Condition = CopyCondition.New,
                        AcquiredDate = DateTime.Today
                    });
                }
                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            DeleteCoverFile(coverPath);
            throw;
        }

        TempData["Success"] = $"Đã thêm sách \"{book.Title}\" kèm {vm.InitialCopies} bản sao.";
        return RedirectToAction(nameof(Details), new { id = book.Id });
    }

    // ---------- Sửa ----------
    public async Task<IActionResult> Edit(int id)
    {
        var book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
        if (book == null) return NotFound();

        var vm = new BookFormVm
        {
            Id = book.Id,
            Title = book.Title,
            ISBN = book.ISBN,
            AuthorId = book.AuthorId,
            CategoryId = book.CategoryId,
            Publisher = book.Publisher,
            PublishYear = book.PublishYear,
            PageCount = book.PageCount,
            RentalFee = book.RentalFee,
            ReplacementPrice = book.ReplacementPrice,
            Description = book.Description,
            ExistingCoverPath = book.CoverImagePath,
            IsActive = book.IsActive,
            InitialCopies = 0
        };
        await FillListsAsync(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BookFormVm vm)
    {
        if (id != vm.Id) return NotFound();

        var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == id);
        if (book == null) return NotFound();

        vm.ISBN = vm.ISBN?.Trim() ?? string.Empty;

        if (await _db.Books.AnyAsync(b => b.ISBN == vm.ISBN && b.Id != id))
            ModelState.AddModelError(nameof(vm.ISBN), "ISBN này đã tồn tại.");

        ValidateCover(vm.CoverFile);
        await ValidateReferencesAsync(vm);

        if (!ModelState.IsValid)
        {
            vm.ExistingCoverPath = book.CoverImagePath;
            await FillListsAsync(vm);
            return View(vm);
        }

        var oldCover = book.CoverImagePath;

        book.Title = vm.Title.Trim();
        book.ISBN = vm.ISBN;
        book.AuthorId = vm.AuthorId;
        book.CategoryId = vm.CategoryId;
        book.Publisher = vm.Publisher;
        book.PublishYear = vm.PublishYear;
        book.PageCount = vm.PageCount;
        book.RentalFee = vm.RentalFee;
        book.ReplacementPrice = vm.ReplacementPrice;
        book.Description = vm.Description;
        book.IsActive = vm.IsActive;

        if (vm.CoverFile is { Length: > 0 })
            book.CoverImagePath = await SaveCoverAsync(vm.CoverFile);
        else if (vm.RemoveCover)
            book.CoverImagePath = null;

        await _db.SaveChangesAsync();

        if (book.CoverImagePath != oldCover)
            DeleteCoverFile(oldCover);

        TempData["Success"] = "Đã cập nhật sách.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---------- Xóa (chỉ khi chưa có bản sao nào) ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var book = await _db.Books.Include(b => b.Copies).FirstOrDefaultAsync(b => b.Id == id);
        if (book == null) return NotFound();

        if (book.Copies.Any())
        {
            TempData["Error"] = "Không thể xóa sách đã có bản sao (cần giữ lịch sử mượn trả). " +
                                "Hãy vào Sửa và bỏ chọn \"Đang kinh doanh\" để ngừng cho mượn.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var cover = book.CoverImagePath;
        _db.Books.Remove(book);
        await _db.SaveChangesAsync();
        DeleteCoverFile(cover);

        TempData["Success"] = $"Đã xóa sách \"{book.Title}\".";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Hàm hỗ trợ ----------
    private async Task FillListsAsync(BookFormVm vm)
    {
        var authors = await _db.Authors.AsNoTracking().OrderBy(a => a.Name)
            .Select(a => new { a.Id, a.Name }).ToListAsync();
        vm.Authors = authors.Select(a => new SelectListItem(a.Name, a.Id.ToString(), a.Id == vm.AuthorId)).ToList();

        vm.Categories = await CategoryItemsAsync(vm.CategoryId);
    }

    private async Task<List<SelectListItem>> CategoryItemsAsync(int? selected)
    {
        var cats = await _db.Categories.AsNoTracking().OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name }).ToListAsync();
        return cats.Select(c => new SelectListItem(c.Name, c.Id.ToString(), selected == c.Id)).ToList();
    }

    private async Task ValidateReferencesAsync(BookFormVm vm)
    {
        if (vm.AuthorId > 0 && !await _db.Authors.AnyAsync(a => a.Id == vm.AuthorId))
            ModelState.AddModelError(nameof(vm.AuthorId), "Tác giả không hợp lệ.");
        if (vm.CategoryId > 0 && !await _db.Categories.AnyAsync(c => c.Id == vm.CategoryId))
            ModelState.AddModelError(nameof(vm.CategoryId), "Thể loại không hợp lệ.");
    }

    private void ValidateCover(IFormFile? file)
    {
        if (file == null || file.Length == 0) return;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            ModelState.AddModelError(nameof(BookFormVm.CoverFile), "Chỉ chấp nhận ảnh .jpg, .jpeg, .png hoặc .webp.");
        else if (file.Length > MaxCoverBytes)
            ModelState.AddModelError(nameof(BookFormVm.CoverFile), "Ảnh bìa tối đa 2MB.");
    }

    private async Task<string> SaveCoverAsync(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var folder = Path.Combine(_env.WebRootPath, "covers");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        await using var stream = System.IO.File.Create(Path.Combine(folder, fileName));
        await file.CopyToAsync(stream);

        return "/covers/" + fileName;
    }

    private void DeleteCoverFile(string? path)
    {
        if (string.IsNullOrEmpty(path) || !path.StartsWith("/covers/")) return;

        var full = Path.Combine(_env.WebRootPath, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(full))
            System.IO.File.Delete(full);
    }
}
