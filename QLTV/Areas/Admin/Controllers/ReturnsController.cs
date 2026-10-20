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

// Trả sách: tính phí thuê, phạt trễ, đền sách hỏng hoặc mất
[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class ReturnsController : Controller
{
    private const int PageSize = 10;
    private static readonly CultureInfo Vi = new("vi-VN");

    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public ReturnsController(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // ---------- Quét mã vạch + danh sách sách đang mượn ----------
    public async Task<IActionResult> Index(string? q, bool overdueOnly = false, int page = 1)
    {
        var today = DateTime.Today;
        var query = _db.LoanItems.AsNoTracking().Where(i => i.ReturnedAt == null);

        var overdueTotal = await query.CountAsync(i => i.DueDate < today);

        if (overdueOnly)
            query = query.Where(i => i.DueDate < today);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(i => i.Loan!.Member!.FullName.Contains(q)
                                     || i.Loan!.Member!.MemberCode.Contains(q)
                                     || i.Loan!.Member!.Phone.Contains(q)
                                     || i.BookCopy!.Barcode.Contains(q)
                                     || i.BookCopy!.Book!.Title.Contains(q));
        }

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = await query
            .OrderBy(i => i.DueDate)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(i => new OpenItemRow
            {
                ItemId = i.Id,
                LoanId = i.LoanId,
                MemberId = i.Loan!.MemberId,
                MemberName = i.Loan!.Member!.FullName,
                MemberCode = i.Loan!.Member!.MemberCode,
                Phone = i.Loan!.Member!.Phone,
                Barcode = i.BookCopy!.Barcode,
                BookTitle = i.BookCopy!.Book!.Title,
                LoanDate = i.Loan!.LoanDate,
                DueDate = i.DueDate,
                RenewCount = i.RenewCount
            })
            .ToListAsync();

        return View(new ReturnIndexVm
        {
            Result = new PagedResult<OpenItemRow> { Items = items, Page = page, PageSize = PageSize, TotalItems = total },
            Q = q,
            OverdueOnly = overdueOnly,
            OverdueTotal = overdueTotal
        });
    }

    // Quét mã vạch: tìm dòng mượn chưa trả của bản sao đó
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Find(string? barcode)
    {
        var code = barcode?.Trim();
        if (string.IsNullOrEmpty(code))
        {
            TempData["Error"] = "Vui lòng nhập hoặc quét mã vạch.";
            return RedirectToAction(nameof(Index));
        }

        var itemId = await _db.LoanItems.AsNoTracking()
            .Where(i => i.ReturnedAt == null && i.BookCopy!.Barcode == code)
            .Select(i => (int?)i.Id)
            .FirstOrDefaultAsync();

        if (itemId == null)
        {
            var exists = await _db.BookCopies.AnyAsync(c => c.Barcode == code);
            TempData["Error"] = exists
                ? $"Bản sao {code} hiện không ở trạng thái đang mượn."
                : $"Không tìm thấy mã vạch {code}.";
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Process), new { loanItemId = itemId.Value });
    }

    // ---------- Xác nhận trả một cuốn ----------
    public async Task<IActionResult> Process(int loanItemId)
    {
        var vm = await BuildAsync(loanItemId);
        if (vm == null)
        {
            TempData["Error"] = "Dòng mượn này không tồn tại hoặc sách đã được trả.";
            return RedirectToAction(nameof(Index));
        }
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Process(int loanItemId, ReturnVm input)
    {
        var item = await _db.LoanItems
            .Include(i => i.Loan)
            .Include(i => i.BookCopy).ThenInclude(c => c!.Book)
            .FirstOrDefaultAsync(i => i.Id == loanItemId && i.ReturnedAt == null);
        if (item == null)
        {
            TempData["Error"] = "Dòng mượn này không tồn tại hoặc sách đã được trả.";
            return RedirectToAction(nameof(Index));
        }

        var copy = item.BookCopy!;
        var book = copy.Book!;
        var today = DateTime.Today;
        var now = DateTime.Now;

        // Tính lại phí từ dữ liệu trong database, không tin số gửi lên từ form
        var perDay = LateFeePerDay();
        int daysLate = Math.Max(0, (today - item.DueDate.Date).Days);
        decimal late = daysLate * perDay;
        if (book.ReplacementPrice > 0) late = Math.Min(late, book.ReplacementPrice);   // phạt không vượt giá sách
        decimal rental = item.RentalFeeCharged;

        if (input.Outcome == ReturnOutcome.Lost && input.CompensationAmount <= 0 && book.ReplacementPrice > 0)
            ModelState.AddModelError(nameof(input.CompensationAmount), "Sách mất cần nhập tiền đền (gợi ý: giá đền của sách).");

        if (!ModelState.IsValid)
        {
            var vm = await BuildAsync(loanItemId);
            vm!.Outcome = input.Outcome;
            vm.Condition = input.Condition;
            vm.CompensationAmount = input.CompensationAmount;
            vm.CollectNow = input.CollectNow;
            vm.Method = input.Method;
            return View(vm);
        }

        // Khoản phải thu theo từng trường hợp
        var charges = new List<(PaymentType Type, decimal Amount, string Note)>();
        switch (input.Outcome)
        {
            case ReturnOutcome.Normal:
                if (rental > 0) charges.Add((PaymentType.RentalFee, rental, "Phí thuê sách"));
                if (late > 0) charges.Add((PaymentType.LateFine, late, $"Trả trễ {daysLate} ngày"));
                break;
            case ReturnOutcome.Damaged:
                if (rental > 0) charges.Add((PaymentType.RentalFee, rental, "Phí thuê sách"));
                if (late > 0) charges.Add((PaymentType.LateFine, late, $"Trả trễ {daysLate} ngày"));
                if (input.CompensationAmount > 0) charges.Add((PaymentType.Compensation, input.CompensationAmount, "Đền sách hỏng"));
                break;
            case ReturnOutcome.Lost:
                // Sách mất: chỉ thu tiền đền
                if (input.CompensationAmount > 0) charges.Add((PaymentType.Compensation, input.CompensationAmount, "Đền sách mất"));
                break;
        }

        // Cập nhật dòng mượn và bản sao
        item.ReturnedAt = now;
        switch (input.Outcome)
        {
            case ReturnOutcome.Normal:
                item.Status = LoanItemStatus.Returned;
                item.ConditionOnReturn = input.Condition;
                copy.Status = CopyStatus.Available;
                copy.Condition = input.Condition;
                break;
            case ReturnOutcome.Damaged:
                item.Status = LoanItemStatus.Returned;
                item.ConditionOnReturn = CopyCondition.Worn;
                copy.Status = CopyStatus.Damaged;
                copy.Condition = CopyCondition.Worn;
                break;
            case ReturnOutcome.Lost:
                item.Status = LoanItemStatus.Lost;
                item.ConditionOnReturn = null;
                copy.Status = CopyStatus.Lost;
                break;
        }

        // Ghi sổ giao dịch (Paid nếu thu ngay, Unpaid nếu ghi nợ)
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var receipts = await CodeGenerator.NextReceiptNosAsync(_db, charges.Count);
        for (int i = 0; i < charges.Count; i++)
        {
            _db.Payments.Add(new Payment
            {
                ReceiptNo = receipts[i],
                Type = charges[i].Type,
                Amount = charges[i].Amount,
                Method = input.Method,
                Status = input.CollectNow ? PaymentStatus.Paid : PaymentStatus.Unpaid,
                MemberId = item.Loan!.MemberId,
                LoanItemId = item.Id,
                ReceivedById = adminId,
                CreatedAt = now,
                PaidAt = input.CollectNow ? now : null,
                Note = charges[i].Note
            });
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception ex) when (ex is DbUpdateConcurrencyException or DbUpdateException)
        {
            TempData["Error"] = "Dòng mượn này vừa được xử lý bởi người khác. Vui lòng kiểm tra lại.";
            return RedirectToAction(nameof(Index));
        }

        var total = charges.Sum(c => c.Amount);
        var what = input.Outcome switch
        {
            ReturnOutcome.Damaged => "trả (sách hỏng)",
            ReturnOutcome.Lost => "ghi nhận mất",
            _ => "trả"
        };
        var money = total == 0
            ? "Không phát sinh phí."
            : input.CollectNow
                ? $"Đã thu {total.ToString("N0", Vi)} đ (biên lai {string.Join(", ", receipts)})."
                : $"Đã ghi nợ {total.ToString("N0", Vi)} đ, khách cần thanh toán trước khi mượn tiếp.";
        TempData["Success"] = $"Đã {what} \"{book.Title}\" ({copy.Barcode}). {money}";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Hàm hỗ trợ ----------
    private decimal LateFeePerDay() => _config.GetValue<decimal?>("Library:LateFeePerDay") ?? 2000m;

    private async Task<ReturnVm?> BuildAsync(int loanItemId)
    {
        var item = await _db.LoanItems.AsNoTracking()
            .Include(i => i.Loan).ThenInclude(l => l!.Member)
            .Include(i => i.BookCopy).ThenInclude(c => c!.Book)
            .FirstOrDefaultAsync(i => i.Id == loanItemId && i.ReturnedAt == null);
        if (item == null) return null;

        var book = item.BookCopy!.Book!;
        var perDay = LateFeePerDay();
        int daysLate = Math.Max(0, (DateTime.Today - item.DueDate.Date).Days);
        decimal late = daysLate * perDay;
        if (book.ReplacementPrice > 0) late = Math.Min(late, book.ReplacementPrice);

        return new ReturnVm
        {
            LoanItemId = item.Id,
            LoanId = item.LoanId,
            MemberId = item.Loan!.MemberId,
            BookTitle = book.Title,
            Barcode = item.BookCopy.Barcode,
            MemberName = item.Loan.Member!.FullName,
            MemberCode = item.Loan.Member.MemberCode,
            LoanDate = item.Loan.LoanDate,
            DueDate = item.DueDate,
            DaysLate = daysLate,
            RentalFee = item.RentalFeeCharged,
            LateFee = late,
            LateFeePerDay = perDay,
            ReplacementPrice = book.ReplacementPrice
        };
    }
}
