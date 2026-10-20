using LibraryManagement.Areas.Admin.Services;
using LibraryManagement.Areas.Admin.ViewModels;
using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class BookCopiesController : Controller
{
    private readonly ApplicationDbContext _db;

    public BookCopiesController(ApplicationDbContext db) => _db = db;

    // ---------- Thêm bản sao cho một đầu sách ----------
    public async Task<IActionResult> Create(int bookId)
    {
        var book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookId);
        if (book == null) return NotFound();

        return View(new AddCopiesVm { BookId = book.Id, BookTitle = book.Title });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AddCopiesVm vm)
    {
        var book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == vm.BookId);
        if (book == null) return NotFound();

        vm.BookTitle = book.Title;
        if (!ModelState.IsValid) return View(vm);

        var barcodes = await BarcodeGenerator.NextAsync(_db, book.Id, vm.Quantity);
        foreach (var code in barcodes)
        {
            _db.BookCopies.Add(new BookCopy
            {
                BookId = book.Id,
                Barcode = code,
                ShelfLocation = vm.ShelfLocation,
                Condition = vm.Condition,
                PurchasePrice = vm.PurchasePrice,
                AcquiredDate = vm.AcquiredDate,
                ReadOnSiteOnly = vm.ReadOnSiteOnly
            });
        }
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã thêm {barcodes.Count} bản sao: {string.Join(", ", barcodes)}.";
        return RedirectToAction("Details", "Books", new { area = "Admin", id = book.Id });
    }

    // ---------- Sửa bản sao: trạng thái, tình trạng, vị trí kệ ----------
    public async Task<IActionResult> Edit(int id)
    {
        var copy = await _db.BookCopies.AsNoTracking().Include(c => c.Book)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (copy == null) return NotFound();

        var vm = new EditCopyVm
        {
            Id = copy.Id,
            Status = copy.Status,
            Condition = copy.Condition,
            ShelfLocation = copy.ShelfLocation,
            ReadOnSiteOnly = copy.ReadOnSiteOnly,
            PurchasePrice = copy.PurchasePrice,
            AcquiredDate = copy.AcquiredDate
        };
        FillInfo(vm, copy);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditCopyVm vm)
    {
        if (id != vm.Id) return NotFound();

        var copy = await _db.BookCopies.Include(c => c.Book).FirstOrDefaultAsync(c => c.Id == id);
        if (copy == null) return NotFound();

        bool onLoan = copy.Status == CopyStatus.OnLoan;

        // "Đang mượn" chỉ do quy trình mượn trả đặt, Admin không đặt tay được
        if (!onLoan && vm.Status == CopyStatus.OnLoan)
            ModelState.AddModelError(nameof(vm.Status),
                "Trạng thái \"Đang mượn\" chỉ được đặt khi cho mượn sách tại quầy.");

        if (!ModelState.IsValid)
        {
            FillInfo(vm, copy);
            return View(vm);
        }

        copy.Condition = vm.Condition;
        copy.ShelfLocation = vm.ShelfLocation;
        copy.ReadOnSiteOnly = vm.ReadOnSiteOnly;
        copy.PurchasePrice = vm.PurchasePrice;
        copy.AcquiredDate = vm.AcquiredDate;
        if (!onLoan)
            copy.Status = vm.Status;

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã cập nhật bản sao {copy.Barcode}.";
        return RedirectToAction("Details", "Books", new { area = "Admin", id = copy.BookId });
    }

    private static void FillInfo(EditCopyVm vm, BookCopy copy)
    {
        vm.BookId = copy.BookId;
        vm.BookTitle = copy.Book?.Title;
        vm.Barcode = copy.Barcode;
        vm.IsOnLoan = copy.Status == CopyStatus.OnLoan;
    }
}
