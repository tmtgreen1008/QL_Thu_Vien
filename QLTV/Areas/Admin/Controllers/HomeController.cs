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
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var revenueTypes = PaymentRules.RevenueTypes;

        var vm = new DashboardVm
        {
            BookTitles = await _db.Books.CountAsync(b => b.IsActive),
            TotalCopies = await _db.BookCopies.CountAsync(c => c.Status != CopyStatus.Disposed),
            AvailableCopies = await _db.BookCopies.CountAsync(c => c.Status == CopyStatus.Available),
            Members = await _db.Members.CountAsync(),
            BorrowedItems = await _db.LoanItems.CountAsync(i => i.ReturnedAt == null),
            OverdueItems = await _db.LoanItems.CountAsync(i => i.ReturnedAt == null && i.DueDate < today),
            RevenueToday = await _db.Payments
                .Where(p => p.Status == PaymentStatus.Paid && revenueTypes.Contains(p.Type)
                            && p.PaidAt >= today && p.PaidAt < tomorrow)
                .SumAsync(p => (decimal?)p.Amount) ?? 0,
            RevenueThisMonth = await _db.Payments
                .Where(p => p.Status == PaymentStatus.Paid && revenueTypes.Contains(p.Type)
                            && p.PaidAt >= monthStart && p.PaidAt < tomorrow)
                .SumAsync(p => (decimal?)p.Amount) ?? 0,
            HeldDeposits = await _db.Memberships
                .Where(m => m.DepositStatus == DepositStatus.Held)
                .SumAsync(m => (decimal?)m.DepositAmount) ?? 0
        };

        return View(vm);
    }
}
