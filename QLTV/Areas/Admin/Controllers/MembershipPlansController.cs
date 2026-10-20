using LibraryManagement.Areas.Admin.ViewModels;
using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class MembershipPlansController : Controller
{
    private readonly ApplicationDbContext _db;

    public MembershipPlansController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var rows = await _db.MembershipPlans.AsNoTracking()
            .OrderByDescending(p => p.IsActive)
            .ThenBy(p => p.Price)
            .Select(p => new PlanRow
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                DurationMonths = p.DurationMonths,
                DepositAmount = p.DepositAmount,
                MaxBooks = p.MaxBooks,
                LoanDays = p.LoanDays,
                MaxRenewals = p.MaxRenewals,
                IsActive = p.IsActive,
                CardCount = p.Memberships.Count
            })
            .ToListAsync();
        return View(rows);
    }

    public IActionResult Create() => View(new MembershipPlan());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Name,Price,DurationMonths,DepositAmount,MaxBooks,LoanDays,MaxRenewals,IsActive")] MembershipPlan plan)
    {
        if (await _db.MembershipPlans.AnyAsync(p => p.Name == plan.Name))
            ModelState.AddModelError(nameof(MembershipPlan.Name), "Tên gói này đã tồn tại.");

        if (!ModelState.IsValid) return View(plan);

        _db.MembershipPlans.Add(plan);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm gói \"{plan.Name}\".";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var plan = await _db.MembershipPlans.FindAsync(id);
        if (plan == null) return NotFound();
        return View(plan);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,
        [Bind("Id,Name,Price,DurationMonths,DepositAmount,MaxBooks,LoanDays,MaxRenewals,IsActive")] MembershipPlan plan)
    {
        if (id != plan.Id) return NotFound();

        if (await _db.MembershipPlans.AnyAsync(p => p.Name == plan.Name && p.Id != id))
            ModelState.AddModelError(nameof(MembershipPlan.Name), "Tên gói này đã tồn tại.");

        if (!ModelState.IsValid) return View(plan);

        var existing = await _db.MembershipPlans.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Name = plan.Name;
        existing.Price = plan.Price;
        existing.DurationMonths = plan.DurationMonths;
        existing.DepositAmount = plan.DepositAmount;
        existing.MaxBooks = plan.MaxBooks;
        existing.LoanDays = plan.LoanDays;
        existing.MaxRenewals = plan.MaxRenewals;
        existing.IsActive = plan.IsActive;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã cập nhật gói.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var plan = await _db.MembershipPlans.FindAsync(id);
        if (plan == null) return NotFound();

        if (await _db.Memberships.AnyAsync(m => m.PlanId == id))
        {
            TempData["Error"] = $"Gói \"{plan.Name}\" đã có thẻ được bán nên không xóa được. " +
                                "Hãy vào Sửa và bỏ chọn \"Đang bán\" để ngừng bán gói này.";
            return RedirectToAction(nameof(Index));
        }

        _db.MembershipPlans.Remove(plan);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã xóa gói \"{plan.Name}\".";
        return RedirectToAction(nameof(Index));
    }
}
