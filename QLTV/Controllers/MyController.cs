using System.Security.Claims;
using LibraryManagement.Areas.Admin.Services;
using LibraryManagement.Areas.Admin.ViewModels;
using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Models.Enums;
using LibraryManagement.Services;
using LibraryManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers;

// Khu vực của khách hàng (role User): chỉ xem và thao tác trên dữ liệu của chính mình
[Authorize(Roles = AppRoles.User)]
public class MyController : Controller
{
    private const int PageSize = 10;
    private readonly ApplicationDbContext _db;

    private readonly Loc _t;

    public MyController(ApplicationDbContext db, Loc t)
    {
        _db = db;
        _t = t;
    }

    // Luôn lấy hồ sơ từ tài khoản đang đăng nhập, không bao giờ nhận memberId từ URL hay form
    private async Task<Member?> CurrentMemberAsync()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return null;
        return await _db.Members.AsNoTracking().FirstOrDefaultAsync(m => m.UserId == userId);
    }

    public async Task<IActionResult> Index()
    {
        var member = await CurrentMemberAsync();
        if (member == null) return View("NoProfile");

        var today = DateTime.Today;
        var elig = await LoanRules.CheckAsync(_db, member.Id, today);
        var cards = await _db.Memberships.AsNoTracking().Where(m => m.MemberId == member.Id).ToListAsync();

        return View(new MyDashboardVm
        {
            Member = member,
            Eligibility = elig,
            HeldDeposit = cards.Where(c => c.DepositStatus == DepositStatus.Held).Sum(c => c.DepositAmount),
            ValidUntil = cards.Where(c => c.Status == MembershipStatus.Active)
                .Select(c => (DateTime?)c.EndDate).Max(),
            DueSoon = await _db.LoanItems.CountAsync(i =>
                i.Loan!.MemberId == member.Id && i.ReturnedAt == null
                && i.DueDate >= today && i.DueDate <= today.AddDays(3))
        });
    }

    // ---------- Sách đang mượn + gia hạn ----------
    public async Task<IActionResult> Loans()
    {
        var member = await CurrentMemberAsync();
        if (member == null) return View("NoProfile");

        var elig = await LoanRules.CheckAsync(_db, member.Id, DateTime.Today);

        var items = await _db.LoanItems.AsNoTracking()
            .Where(i => i.Loan!.MemberId == member.Id && i.ReturnedAt == null)
            .OrderBy(i => i.DueDate)
            .Select(i => new MyLoanRow
            {
                ItemId = i.Id,
                Title = i.BookCopy!.Book!.Title,
                Author = i.BookCopy!.Book!.Author!.Name,
                Barcode = i.BookCopy!.Barcode,
                LoanDate = i.Loan!.LoanDate,
                DueDate = i.DueDate,
                RenewCount = i.RenewCount,
                RentalFee = i.RentalFeeCharged
            })
            .ToListAsync();

        return View(new MyLoansVm
        {
            Items = items,
            MaxRenewals = elig.Card?.Plan?.MaxRenewals ?? 0,
            HasValidCard = elig.Card != null
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Renew(int itemId)
    {
        var member = await CurrentMemberAsync();
        if (member == null) return Forbid();

        // Kiểm tra quyền sở hữu: chỉ gia hạn được sách của chính mình (chống truy cập chéo dữ liệu)
        var item = await _db.LoanItems
            .Include(i => i.Loan)
            .Include(i => i.BookCopy).ThenInclude(c => c!.Book)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.Loan!.MemberId == member.Id);
        if (item == null) return NotFound();

        var decision = await LoanRules.CheckRenewAsync(_db, item, DateTime.Today);
        var title = item.BookCopy?.Book?.Title;

        if (decision.Error != null)
        {
            TempData["Error"] = _t["loans.renewed.fail", title ?? "", RenewMessages.Localize(_t, decision)];
        }
        else
        {
            item.DueDate = item.DueDate.Date.AddDays(decision.AddDays);
            item.RenewCount++;
            await _db.SaveChangesAsync();
            TempData["Success"] = _t["loans.renewed.ok", title ?? "", decision.AddDays, item.DueDate.ToString("d")];
        }
        return RedirectToAction(nameof(Loans));
    }

    // ---------- Lịch sử mượn trả ----------
    public async Task<IActionResult> History(int page = 1)
    {
        var member = await CurrentMemberAsync();
        if (member == null) return View("NoProfile");

        var query = _db.LoanItems.AsNoTracking()
            .Where(i => i.Loan!.MemberId == member.Id && i.ReturnedAt != null);

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = await query.OrderByDescending(i => i.ReturnedAt)
            .Skip((page - 1) * PageSize).Take(PageSize)
            .Select(i => new MyHistoryRow
            {
                Title = i.BookCopy!.Book!.Title,
                LoanDate = i.Loan!.LoanDate,
                ReturnedAt = i.ReturnedAt,
                Lost = i.Status == LoanItemStatus.Lost
            })
            .ToListAsync();

        return View(new MyHistoryVm
        {
            Result = new PagedResult<MyHistoryRow> { Items = items, Page = page, PageSize = PageSize, TotalItems = total }
        });
    }

    // ---------- Thanh toán và khoản nợ ----------
    public async Task<IActionResult> Payments(int page = 1)
    {
        var member = await CurrentMemberAsync();
        if (member == null) return View("NoProfile");

        var query = _db.Payments.AsNoTracking().Where(p => p.MemberId == member.Id);

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = await query.OrderByDescending(p => p.PaidAt ?? p.CreatedAt).ThenByDescending(p => p.Id)
            .Skip((page - 1) * PageSize).Take(PageSize)
            .Select(p => new PaymentRow
            {
                Id = p.Id,
                ReceiptNo = p.ReceiptNo,
                When = p.PaidAt ?? p.CreatedAt,
                Type = p.Type,
                Amount = p.Amount,
                Method = p.Method,
                Status = p.Status,
                Note = p.Note
            })
            .ToListAsync();

        return View(new MyPaymentsVm
        {
            Result = new PagedResult<PaymentRow> { Items = items, Page = page, PageSize = PageSize, TotalItems = total },
            UnpaidTotal = await query.Where(p => p.Status == PaymentStatus.Unpaid)
                .SumAsync(p => (decimal?)p.Amount) ?? 0
        });
    }

    // ---------- Hồ sơ cá nhân ----------
    public async Task<IActionResult> Profile()
    {
        var member = await CurrentMemberAsync();
        if (member == null) return View("NoProfile");

        return View(new MyProfileVm
        {
            MemberCode = member.MemberCode,
            Phone = member.Phone,
            FullName = member.FullName,
            Email = member.Email,
            Address = member.Address,
            DateOfBirth = member.DateOfBirth
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(MyProfileVm vm)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var member = await _db.Members.FirstOrDefaultAsync(m => m.UserId == userId);
        if (member == null) return Forbid();

        // Mã thành viên và số điện thoại không cho tự sửa (đổi tại quầy) vì dùng để xác minh danh tính
        vm.MemberCode = member.MemberCode;
        vm.Phone = member.Phone;

        if (vm.DateOfBirth.HasValue && vm.DateOfBirth.Value.Date > DateTime.Today)
            ModelState.AddModelError(nameof(vm.DateOfBirth), _t["profile.dob.future"]);
        if (!ModelState.IsValid) return View(vm);

        member.FullName = vm.FullName.Trim();
        member.Email = vm.Email;
        member.Address = vm.Address;
        member.DateOfBirth = vm.DateOfBirth;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user != null) user.FullName = member.FullName;

        await _db.SaveChangesAsync();
        TempData["Success"] = _t["profile.saved"];
        return RedirectToAction(nameof(Profile));
    }
}
