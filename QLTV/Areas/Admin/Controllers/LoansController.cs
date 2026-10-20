using LibraryManagement.Areas.Admin.Services;
using LibraryManagement.Areas.Admin.ViewModels;
using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LibraryManagement.Areas.Admin.Controllers;

// Cho mượn sách tại quầy, xem phiếu mượn, gia hạn
[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class LoansController : Controller
{
    private const int PageSize = 10;
    private readonly ApplicationDbContext _db;

    public LoansController(ApplicationDbContext db) => _db = db;

    // ---------- Danh sách phiếu mượn ----------
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var today = DateTime.Today;
        var query = _db.Loans.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(l => l.Member!.FullName.Contains(search)
                                     || l.Member!.MemberCode.Contains(search)
                                     || l.Member!.Phone.Contains(search));
        }

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = await query
            .OrderByDescending(l => l.LoanDate)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(l => new LoanRow
            {
                Id = l.Id,
                LoanDate = l.LoanDate,
                MemberName = l.Member!.FullName,
                MemberCode = l.Member!.MemberCode,
                AdminName = l.Admin!.FullName,
                ItemCount = l.Items.Count,
                OpenCount = l.Items.Count(i => i.ReturnedAt == null),
                OverdueCount = l.Items.Count(i => i.ReturnedAt == null && i.DueDate < today)
            })
            .ToListAsync();

        return View(new LoanIndexVm
        {
            Result = new PagedResult<LoanRow> { Items = items, Page = page, PageSize = PageSize, TotalItems = total },
            Search = search
        });
    }

    // ---------- Chi tiết phiếu mượn ----------
    public async Task<IActionResult> Details(int id)
    {
        var loan = await _db.Loans.AsNoTracking()
            .Include(l => l.Member)
            .Include(l => l.Admin)
            .Include(l => l.Membership).ThenInclude(m => m!.Plan)
            .Include(l => l.Items).ThenInclude(i => i.BookCopy).ThenInclude(c => c!.Book)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (loan == null) return NotFound();
        return View(loan);
    }

    public async Task<IActionResult> Print(int id)
    {
        var loan = await _db.Loans.AsNoTracking()
            .Include(l => l.Member)
            .Include(l => l.Admin)
            .Include(l => l.Items).ThenInclude(i => i.BookCopy).ThenInclude(c => c!.Book)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (loan == null) return NotFound();
        return View(loan);
    }

    // ---------- Cho mượn: tìm khách rồi nhập mã vạch ----------
    public async Task<IActionResult> Create(int? memberId, string? q)
    {
        var vm = new LoanCreateVm { Q = q };

        if (memberId.HasValue)
        {
            if (!await _db.Members.AnyAsync(m => m.Id == memberId.Value)) return NotFound();
            vm.Eligibility = await LoanRules.CheckAsync(_db, memberId.Value, DateTime.Today);
        }
        else if (!string.IsNullOrWhiteSpace(q))
        {
            vm.Matches = await FindMembersAsync(q.Trim());
            if (vm.Matches.Count == 1)
                return RedirectToAction(nameof(Create), new { memberId = vm.Matches[0].Id });
        }

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int memberId, string? barcodes, string? note)
    {
        if (!await _db.Members.AnyAsync(m => m.Id == memberId)) return NotFound();

        var today = DateTime.Today;
        var vm = new LoanCreateVm { Barcodes = barcodes, Note = note };
        var elig = await LoanRules.CheckAsync(_db, memberId, today);
        vm.Eligibility = elig;

        if (!elig.CanBorrow) return View(vm);

        var codes = ParseBarcodes(barcodes);
        if (codes.Count == 0)
        {
            vm.Errors.Add("Vui lòng nhập ít nhất một mã vạch.");
            return View(vm);
        }
        if (codes.Count > elig.Remaining)
        {
            vm.Errors.Add($"Khách chỉ được mượn thêm {elig.Remaining} cuốn nữa (đang mượn {elig.OpenCount}/{elig.MaxBooks}), " +
                          $"bạn đang nhập {codes.Count} mã.");
            return View(vm);
        }

        // Nạp có theo dõi để RowVersion chặn hai Admin cùng cho mượn một bản
        var copies = await _db.BookCopies.Include(c => c.Book)
            .Where(c => codes.Contains(c.Barcode))
            .ToListAsync();

        var selected = new List<BookCopy>();
        foreach (var code in codes)
        {
            var copy = copies.FirstOrDefault(c => string.Equals(c.Barcode, code, StringComparison.OrdinalIgnoreCase));
            if (copy == null) { vm.Errors.Add($"{code}: không tìm thấy mã vạch này."); continue; }

            var title = copy.Book?.Title;
            if (copy.Status != CopyStatus.Available)
                vm.Errors.Add($"{copy.Barcode} ({title}): {StatusText(copy.Status)}.");
            else if (copy.ReadOnSiteOnly)
                vm.Errors.Add($"{copy.Barcode} ({title}): chỉ đọc tại chỗ, không cho mượn về.");
            else if (copy.Book is { IsActive: false })
                vm.Errors.Add($"{copy.Barcode} ({title}): đầu sách đã ngừng kinh doanh.");
            else
                selected.Add(copy);
        }

        if (vm.Errors.Count > 0) return View(vm);

        var plan = elig.Plan!;
        var loan = new Loan
        {
            MemberId = memberId,
            MembershipId = elig.Card!.Id,
            AdminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty,
            LoanDate = DateTime.Now,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
        };

        foreach (var copy in selected)
        {
            loan.Items.Add(new LoanItem
            {
                BookCopy = copy,
                DueDate = today.AddDays(plan.LoanDays),
                RentalFeeCharged = copy.Book!.RentalFee,
                Status = LoanItemStatus.Borrowed
            });
            copy.Status = CopyStatus.OnLoan;
        }

        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            _db.Loans.Add(loan);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (Exception ex) when (ex is DbUpdateConcurrencyException or DbUpdateException)
        {
            vm.Errors.Add("Có bản sao vừa được người khác thao tác. Vui lòng thử lại.");
            vm.Eligibility = await LoanRules.CheckAsync(_db, memberId, today);
            return View(vm);
        }

        TempData["Success"] = $"Đã cho {elig.Member.FullName} mượn {selected.Count} cuốn, hạn trả " +
                              $"{today.AddDays(plan.LoanDays):dd/MM/yyyy}.";
        return RedirectToAction(nameof(Details), new { id = loan.Id });
    }

    // ---------- Gia hạn một cuốn ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Renew(int itemId, string? returnUrl)
    {
        var item = await _db.LoanItems
            .Include(i => i.Loan)
            .Include(i => i.BookCopy).ThenInclude(c => c!.Book)
            .FirstOrDefaultAsync(i => i.Id == itemId);
        if (item == null) return NotFound();

        var today = DateTime.Today;
        var decision = await LoanRules.CheckRenewAsync(_db, item, today);
        var title = item.BookCopy?.Book?.Title;

        if (decision.Error != null)
        {
            TempData["Error"] = $"Không gia hạn được \"{title}\": {decision.Error}";
        }
        else
        {
            item.DueDate = item.DueDate.Date.AddDays(decision.AddDays);
            item.RenewCount++;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã gia hạn \"{title}\" thêm {decision.AddDays} ngày, hạn trả mới {item.DueDate:dd/MM/yyyy}.";
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(nameof(Details), new { id = item.LoanId });
    }

    // ---------- Hàm hỗ trợ ----------
    private async Task<List<MemberMatch>> FindMembersAsync(string q)
    {
        var exact = await _db.Members.AsNoTracking()
            .Where(m => m.MemberCode == q || m.Phone == q)
            .Select(m => new MemberMatch { Id = m.Id, MemberCode = m.MemberCode, FullName = m.FullName, Phone = m.Phone })
            .ToListAsync();
        if (exact.Count > 0) return exact;

        return await _db.Members.AsNoTracking()
            .Where(m => m.FullName.Contains(q))
            .OrderBy(m => m.FullName)
            .Take(10)
            .Select(m => new MemberMatch { Id = m.Id, MemberCode = m.MemberCode, FullName = m.FullName, Phone = m.Phone })
            .ToListAsync();
    }

    private static List<string> ParseBarcodes(string? input) =>
        (input ?? string.Empty)
            .Split(new[] { '\r', '\n', ',', ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string StatusText(CopyStatus s) => s switch
    {
        CopyStatus.OnLoan => "đang được mượn",
        CopyStatus.Lost => "đã mất",
        CopyStatus.Damaged => "đang hỏng",
        CopyStatus.Disposed => "đã thanh lý",
        _ => "không sẵn sàng"
    };
}
