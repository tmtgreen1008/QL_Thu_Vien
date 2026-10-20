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
public class MembersController : Controller
{
    private const int PageSize = 10;
    private readonly ApplicationDbContext _db;

    public MembersController(ApplicationDbContext db) => _db = db;

    // ---------- Danh sách: tìm theo tên, SĐT, mã; lọc theo tình trạng thẻ ----------
    public async Task<IActionResult> Index(string? search, string? filter, int page = 1)
    {
        var today = DateTime.Today;
        var soon = today.AddDays(14);
        var query = _db.Members.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(m => m.FullName.Contains(search)
                                     || m.Phone.Contains(search)
                                     || m.MemberCode.Contains(search));
        }

        query = filter switch
        {
            "valid" => query.Where(m => m.Memberships.Any(x => x.Status == MembershipStatus.Active && x.EndDate >= today)),
            "expiring" => query.Where(m =>
                m.Memberships.Any(x => x.Status == MembershipStatus.Active && x.EndDate >= today)
                && !m.Memberships.Any(x => x.Status == MembershipStatus.Active && x.EndDate > soon)),
            "expired" => query.Where(m =>
                m.Memberships.Any(x => x.Status == MembershipStatus.Active)
                && !m.Memberships.Any(x => x.Status == MembershipStatus.Active && x.EndDate >= today)),
            "none" => query.Where(m => !m.Memberships.Any(x => x.Status == MembershipStatus.Active)),
            "locked" => query.Where(m => m.IsLocked),
            _ => query
        };

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = await query
            .OrderBy(m => m.FullName)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(m => new MemberRow
            {
                Id = m.Id,
                MemberCode = m.MemberCode,
                FullName = m.FullName,
                Phone = m.Phone,
                IsLocked = m.IsLocked,
                ValidUntil = m.Memberships
                    .Where(x => x.Status == MembershipStatus.Active)
                    .Max(x => (DateTime?)x.EndDate)
            })
            .ToListAsync();

        return View(new MemberIndexVm
        {
            Result = new PagedResult<MemberRow>
            {
                Items = items,
                Page = page,
                PageSize = PageSize,
                TotalItems = total
            },
            Search = search,
            Filter = filter
        });
    }

    // ---------- Chi tiết khách ----------
    public async Task<IActionResult> Details(int id)
    {
        var member = await _db.Members.AsNoTracking().Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (member == null) return NotFound();

        var today = DateTime.Today;
        var memberships = await _db.Memberships.AsNoTracking().Include(m => m.Plan)
            .Where(m => m.MemberId == id)
            .OrderByDescending(m => m.StartDate)
            .ToListAsync();

        var vm = new MemberDetailsVm
        {
            Member = member,
            Memberships = memberships,
            CurrentCard = memberships.FirstOrDefault(m =>
                m.Status == MembershipStatus.Active && m.StartDate.Date <= today && m.EndDate.Date >= today),
            HeldDeposit = memberships.Where(m => m.DepositStatus == DepositStatus.Held).Sum(m => m.DepositAmount),
            OpenLoanItems = await _db.LoanItems.CountAsync(i => i.Loan!.MemberId == id && i.ReturnedAt == null),
            UnpaidTotal = await _db.Payments
                .Where(p => p.MemberId == id && p.Status == PaymentStatus.Unpaid)
                .SumAsync(p => (decimal?)p.Amount) ?? 0,
            RecentPayments = await _db.Payments.AsNoTracking()
                .Where(p => p.MemberId == id)
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .ToListAsync(),
            UnpaidPayments = await _db.Payments.AsNoTracking()
                .Where(p => p.MemberId == id && p.Status == PaymentStatus.Unpaid)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync()
        };
        return View(vm);
    }

    // ---------- Tạo hồ sơ khách tại quầy ----------
    public IActionResult Create() => View(new MemberFormVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MemberFormVm vm)
    {
        await ValidateAsync(vm);
        if (!ModelState.IsValid) return View(vm);

        var member = new Member
        {
            MemberCode = await CodeGenerator.NextMemberCodeAsync(_db),
            FullName = vm.FullName.Trim(),
            Phone = vm.Phone,
            Email = vm.Email,
            Address = vm.Address,
            DateOfBirth = vm.DateOfBirth
        };
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã tạo hồ sơ {member.MemberCode} - {member.FullName}. Bán thẻ để khách bắt đầu mượn sách.";
        return RedirectToAction(nameof(Details), new { id = member.Id });
    }

    // ---------- Sửa thông tin ----------
    public async Task<IActionResult> Edit(int id)
    {
        var member = await _db.Members.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
        if (member == null) return NotFound();

        return View(new MemberFormVm
        {
            Id = member.Id,
            MemberCode = member.MemberCode,
            FullName = member.FullName,
            Phone = member.Phone,
            Email = member.Email,
            Address = member.Address,
            DateOfBirth = member.DateOfBirth
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MemberFormVm vm)
    {
        if (id != vm.Id) return NotFound();

        var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == id);
        if (member == null) return NotFound();

        vm.MemberCode = member.MemberCode;
        await ValidateAsync(vm);
        if (!ModelState.IsValid) return View(vm);

        member.FullName = vm.FullName.Trim();
        member.Phone = vm.Phone;
        member.Email = vm.Email;
        member.Address = vm.Address;
        member.DateOfBirth = vm.DateOfBirth;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã cập nhật thông tin khách.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---------- Khóa / mở khóa ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock(int id, string? reason)
    {
        var member = await _db.Members.FindAsync(id);
        if (member == null) return NotFound();

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "Vui lòng nhập lý do khóa.";
            return RedirectToAction(nameof(Details), new { id });
        }

        member.IsLocked = true;
        member.LockReason = reason.Trim().Length > 250 ? reason.Trim()[..250] : reason.Trim();
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã khóa khách {member.FullName}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(int id)
    {
        var member = await _db.Members.FindAsync(id);
        if (member == null) return NotFound();

        member.IsLocked = false;
        member.LockReason = null;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã mở khóa khách {member.FullName}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task ValidateAsync(MemberFormVm vm)
    {
        vm.Phone = (vm.Phone ?? string.Empty).Trim();

        if (await _db.Members.AnyAsync(m => m.Phone == vm.Phone && m.Id != vm.Id))
            ModelState.AddModelError(nameof(vm.Phone), "Số điện thoại này đã có trong hệ thống.");

        if (vm.DateOfBirth.HasValue && vm.DateOfBirth.Value.Date > DateTime.Today)
            ModelState.AddModelError(nameof(vm.DateOfBirth), "Ngày sinh không được ở tương lai.");
    }
}
