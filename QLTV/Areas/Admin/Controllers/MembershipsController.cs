using LibraryManagement.Areas.Admin.Services;
using LibraryManagement.Areas.Admin.ViewModels;
using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;

namespace LibraryManagement.Areas.Admin.Controllers;

// Bán / gia hạn thẻ thành viên và hoàn cọc, hủy thẻ
[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class MembershipsController : Controller
{
    private static readonly CultureInfo Vi = new("vi-VN");
    private readonly ApplicationDbContext _db;

    public MembershipsController(ApplicationDbContext db) => _db = db;

    // ---------- Bán / gia hạn thẻ ----------
    public async Task<IActionResult> Sell(int memberId)
    {
        var member = await _db.Members.AsNoTracking().FirstOrDefaultAsync(m => m.Id == memberId);
        if (member == null) return NotFound();

        if (member.IsLocked)
        {
            TempData["Error"] = "Khách đang bị khóa, mở khóa trước khi bán hoặc gia hạn thẻ.";
            return RedirectToAction("Details", "Members", new { area = "Admin", id = memberId });
        }

        var vm = new SellMembershipVm { MemberId = memberId };
        await FillAsync(vm, member);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sell(SellMembershipVm vm)
    {
        var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == vm.MemberId);
        if (member == null) return NotFound();

        if (member.IsLocked)
        {
            TempData["Error"] = "Khách đang bị khóa, mở khóa trước khi bán hoặc gia hạn thẻ.";
            return RedirectToAction("Details", "Members", new { area = "Admin", id = member.Id });
        }

        var plan = await _db.MembershipPlans.FirstOrDefaultAsync(p => p.Id == vm.PlanId && p.IsActive);
        if (plan == null)
            ModelState.AddModelError(nameof(vm.PlanId), "Gói không hợp lệ hoặc đã ngừng bán.");

        if (!ModelState.IsValid)
        {
            await FillAsync(vm, member);
            return View(vm);
        }

        var today = DateTime.Today;
        var cards = await _db.Memberships.Where(m => m.MemberId == member.Id).ToListAsync();

        // Gia hạn: thẻ mới bắt đầu ngay sau khi thẻ hiện tại hết hạn
        var lastEnd = cards.Where(c => c.Status == MembershipStatus.Active && c.EndDate.Date >= today)
            .Select(c => (DateTime?)c.EndDate.Date).Max();
        var start = lastEnd.HasValue ? lastEnd.Value.AddDays(1) : today;

        // Tiền cọc chỉ thu một lần, khi khách chưa có cọc nào đang giữ
        bool chargeDeposit = plan!.DepositAmount > 0
                             && !cards.Any(c => c.DepositStatus == DepositStatus.Held && c.DepositAmount > 0);

        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var now = DateTime.Now;

        var membership = new Membership
        {
            MemberId = member.Id,
            PlanId = plan.Id,
            StartDate = start,
            EndDate = start.AddMonths(plan.DurationMonths),
            PricePaid = plan.Price,
            DepositAmount = chargeDeposit ? plan.DepositAmount : 0,
            DepositStatus = DepositStatus.Held,
            Status = MembershipStatus.Active
        };

        var payments = new List<Payment>();
        if (plan.Price > 0)
            payments.Add(NewPayment(PaymentType.MembershipFee, plan.Price, vm, member.Id, membership, adminId, now, vm.Note));
        if (chargeDeposit)
            payments.Add(NewPayment(PaymentType.Deposit, plan.DepositAmount, vm, member.Id, membership, adminId, now, null));

        var receipts = await CodeGenerator.NextReceiptNosAsync(_db, payments.Count);
        for (int i = 0; i < payments.Count; i++)
            payments[i].ReceiptNo = receipts[i];

        _db.Memberships.Add(membership);
        _db.Payments.AddRange(payments);
        await _db.SaveChangesAsync();   // một lần lưu = thẻ và biên lai cùng thành công hoặc cùng thất bại

        var total = payments.Sum(p => p.Amount);
        var receiptText = payments.Count > 0 ? $" Biên lai: {string.Join(", ", receipts)}." : "";
        TempData["Success"] = $"Đã {(lastEnd.HasValue ? "gia hạn" : "bán")} thẻ \"{plan.Name}\" " +
                              $"({membership.StartDate:dd/MM/yyyy} - {membership.EndDate:dd/MM/yyyy}). " +
                              $"Tổng thu {total.ToString("N0", Vi)} đ.{receiptText}";
        return RedirectToAction("Details", "Members", new { area = "Admin", id = member.Id });
    }

    // ---------- Hoàn cọc và hủy thẻ ----------
    public async Task<IActionResult> Refund(int memberId)
    {
        var vm = await BuildRefundAsync(memberId);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Refund(int memberId, PaymentMethod method)
    {
        var vm = await BuildRefundAsync(memberId);
        if (vm == null) return NotFound();

        if (!vm.CanProceed)
        {
            TempData["Error"] = "Chưa thể hoàn cọc: khách còn sách chưa trả hoặc còn khoản nợ.";
            return RedirectToAction("Details", "Members", new { area = "Admin", id = memberId });
        }

        var cards = await _db.Memberships
            .Where(m => m.MemberId == memberId
                        && (m.Status == MembershipStatus.Active
                            || (m.DepositStatus == DepositStatus.Held && m.DepositAmount > 0)))
            .ToListAsync();

        var heldCards = cards.Where(c => c.DepositStatus == DepositStatus.Held && c.DepositAmount > 0).ToList();
        var held = heldCards.Sum(c => c.DepositAmount);

        foreach (var c in cards)
        {
            c.Status = MembershipStatus.Cancelled;
            if (c.DepositStatus == DepositStatus.Held && c.DepositAmount > 0)
                c.DepositStatus = DepositStatus.Refunded;
        }

        string? receipt = null;
        if (held > 0)
        {
            receipt = (await CodeGenerator.NextReceiptNosAsync(_db, 1))[0];
            var now = DateTime.Now;
            _db.Payments.Add(new Payment
            {
                ReceiptNo = receipt,
                Type = PaymentType.DepositRefund,
                Amount = -held,                       // hoàn cọc ghi số âm
                Method = method,
                Status = PaymentStatus.Paid,
                MemberId = memberId,
                MembershipId = heldCards[0].Id,
                ReceivedById = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
                CreatedAt = now,
                PaidAt = now,
                Note = "Hoàn cọc, hủy thẻ"
            });
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = held > 0
            ? $"Đã hủy thẻ và hoàn cọc {held.ToString("N0", Vi)} đ (biên lai {receipt})."
            : "Đã hủy thẻ.";
        return RedirectToAction("Details", "Members", new { area = "Admin", id = memberId });
    }

    // ---------- Hàm hỗ trợ ----------
    private static Payment NewPayment(PaymentType type, decimal amount, SellMembershipVm vm, int memberId,
        Membership membership, string adminId, DateTime now, string? note) => new()
    {
        Type = type,
        Amount = amount,
        Method = vm.Method,
        Status = PaymentStatus.Paid,
        MemberId = memberId,
        Membership = membership,
        ReceivedById = adminId,
        CreatedAt = now,
        PaidAt = now,
        Note = note
    };

    private async Task FillAsync(SellMembershipVm vm, Member member)
    {
        var today = DateTime.Today;
        var cards = await _db.Memberships.AsNoTracking().Where(m => m.MemberId == member.Id).ToListAsync();

        var lastEnd = cards.Where(c => c.Status == MembershipStatus.Active && c.EndDate.Date >= today)
            .Select(c => (DateTime?)c.EndDate.Date).Max();

        vm.MemberName = member.FullName;
        vm.MemberCode = member.MemberCode;
        vm.IsRenewal = lastEnd.HasValue;
        vm.StartDate = lastEnd.HasValue ? lastEnd.Value.AddDays(1) : today;
        vm.NeedsDeposit = !cards.Any(c => c.DepositStatus == DepositStatus.Held && c.DepositAmount > 0);
        vm.Plans = await _db.MembershipPlans.AsNoTracking()
            .Where(p => p.IsActive).OrderBy(p => p.Price).ToListAsync();
    }

    private async Task<RefundVm?> BuildRefundAsync(int memberId)
    {
        var member = await _db.Members.AsNoTracking().FirstOrDefaultAsync(m => m.Id == memberId);
        if (member == null) return null;

        var cards = await _db.Memberships.AsNoTracking().Where(m => m.MemberId == memberId).ToListAsync();

        return new RefundVm
        {
            MemberId = memberId,
            MemberName = member.FullName,
            MemberCode = member.MemberCode,
            HeldDeposit = cards.Where(c => c.DepositStatus == DepositStatus.Held).Sum(c => c.DepositAmount),
            ActiveCards = cards.Count(c => c.Status == MembershipStatus.Active),
            OpenLoanItems = await _db.LoanItems.CountAsync(i => i.Loan!.MemberId == memberId && i.ReturnedAt == null),
            UnpaidTotal = await _db.Payments
                .Where(p => p.MemberId == memberId && p.Status == PaymentStatus.Unpaid)
                .SumAsync(p => (decimal?)p.Amount) ?? 0
        };
    }
}
