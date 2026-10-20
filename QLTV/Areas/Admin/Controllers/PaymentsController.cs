using LibraryManagement.Areas.Admin.Services;
using LibraryManagement.Areas.Admin.ViewModels;
using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace LibraryManagement.Areas.Admin.Controllers;

// Sổ giao dịch: xem, lọc, xuất CSV, thu nợ, hủy giao dịch
[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class PaymentsController : Controller
{
    private const int PageSize = 15;
    private readonly ApplicationDbContext _db;

    public PaymentsController(ApplicationDbContext db) => _db = db;

    // ---------- Sổ giao dịch ----------
    public async Task<IActionResult> Index(DateTime? dateFrom, DateTime? dateTo, PaymentType? type,
        PaymentMethod? method, PaymentStatus? status, string? q, int page = 1)
    {
        // Lần đầu vào trang (chưa có tham số ngày) mặc định xem từ đầu tháng tới hôm nay
        if (!Request.Query.ContainsKey("dateFrom") && !Request.Query.ContainsKey("dateTo"))
        {
            var today = DateTime.Today;
            dateFrom = new DateTime(today.Year, today.Month, 1);
            dateTo = today;
        }

        var query = Filter(dateFrom, dateTo, type, method, status, q);
        var revenueTypes = PaymentRules.RevenueTypes;
        var depositTypes = PaymentRules.DepositTypes;

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = await query
            .OrderByDescending(p => p.PaidAt ?? p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(p => new PaymentRow
            {
                Id = p.Id,
                ReceiptNo = p.ReceiptNo,
                When = p.PaidAt ?? p.CreatedAt,
                MemberId = p.MemberId,
                MemberName = p.Member!.FullName,
                MemberCode = p.Member!.MemberCode,
                Type = p.Type,
                Amount = p.Amount,
                Method = p.Method,
                Status = p.Status,
                ReceivedByName = p.ReceivedBy!.FullName,
                Note = p.Note,
                VoidReason = p.VoidReason
            })
            .ToListAsync();

        var vm = new PaymentIndexVm
        {
            Result = new PagedResult<PaymentRow> { Items = items, Page = page, PageSize = PageSize, TotalItems = total },
            DateFrom = dateFrom,
            DateTo = dateTo,
            Type = type,
            Method = method,
            Status = status,
            Q = q,
            Revenue = await query.Where(p => p.Status == PaymentStatus.Paid && revenueTypes.Contains(p.Type))
                .SumAsync(p => (decimal?)p.Amount) ?? 0,
            DepositNet = await query.Where(p => p.Status == PaymentStatus.Paid && depositTypes.Contains(p.Type))
                .SumAsync(p => (decimal?)p.Amount) ?? 0,
            UnpaidTotal = await query.Where(p => p.Status == PaymentStatus.Unpaid)
                .SumAsync(p => (decimal?)p.Amount) ?? 0,
            VoidedCount = await query.CountAsync(p => p.Status == PaymentStatus.Voided)
        };
        return View(vm);
    }

    // ---------- Xuất CSV (mở được bằng Excel) ----------
    public async Task<IActionResult> Export(DateTime? dateFrom, DateTime? dateTo, PaymentType? type,
        PaymentMethod? method, PaymentStatus? status, string? q)
    {
        var rows = await Filter(dateFrom, dateTo, type, method, status, q)
            .OrderBy(p => p.PaidAt ?? p.CreatedAt).ThenBy(p => p.Id)
            .Select(p => new
            {
                p.ReceiptNo,
                When = p.PaidAt ?? p.CreatedAt,
                MemberCode = p.Member!.MemberCode,
                MemberName = p.Member!.FullName,
                p.Type,
                p.Amount,
                p.Method,
                p.Status,
                Admin = p.ReceivedBy!.FullName,
                p.Note
            })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Số biên lai,Ngày,Mã khách,Tên khách,Loại,Số tiền,Hình thức,Trạng thái,Người thu,Ghi chú");
        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                Csv(r.ReceiptNo), Csv(r.When.ToString("dd/MM/yyyy HH:mm")), Csv(r.MemberCode), Csv(r.MemberName),
                Csv(PaymentLabels.Type(r.Type)), r.Amount.ToString("0"), Csv(PaymentLabels.Method(r.Method)),
                Csv(PaymentLabels.Status(r.Status)), Csv(r.Admin), Csv(r.Note)
            }));
        }

        var bytes = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"so-giao-dich-{DateTime.Now:yyyyMMdd-HHmm}.csv");
    }

    // ---------- Thu một khoản còn nợ ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Settle(int id, PaymentMethod method)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == id);
        if (payment == null) return NotFound();

        if (payment.Status != PaymentStatus.Unpaid)
        {
            TempData["Error"] = "Khoản này không còn ở trạng thái nợ.";
        }
        else
        {
            payment.Status = PaymentStatus.Paid;
            payment.Method = method;
            payment.PaidAt = DateTime.Now;
            payment.ReceivedById = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? payment.ReceivedById;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã thu khoản {payment.ReceiptNo}: {payment.Amount:N0} đ.";
        }

        if (payment.MemberId.HasValue)
            return RedirectToAction("Details", "Members", new { area = "Admin", id = payment.MemberId.Value });
        return RedirectToAction(nameof(Index));
    }

    // ---------- Hủy giao dịch (bắt buộc ghi lý do, không xóa dòng) ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Void(int id, string? reason, string? returnUrl)
    {
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == id);
        if (payment == null) return NotFound();

        if (payment.Status == PaymentStatus.Voided)
            TempData["Error"] = "Giao dịch này đã bị hủy trước đó.";
        else if (!PaymentRules.CanVoid(payment.Type))
            TempData["Error"] = $"Không hủy trực tiếp khoản \"{PaymentLabels.Type(payment.Type)}\". " +
                                "Hãy dùng chức năng Hoàn cọc, hủy thẻ ở hồ sơ khách.";
        else if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5)
            TempData["Error"] = "Vui lòng nhập lý do hủy (ít nhất 5 ký tự).";
        else
        {
            var text = $"{reason.Trim()} (bởi {User.Identity?.Name}, {DateTime.Now:dd/MM/yyyy HH:mm})";
            payment.Status = PaymentStatus.Voided;
            payment.VoidReason = text.Length > 250 ? text[..250] : text;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Đã hủy giao dịch {payment.ReceiptNo}.";
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }

    // ---------- Hàm hỗ trợ ----------
    private IQueryable<Payment> Filter(DateTime? dateFrom, DateTime? dateTo, PaymentType? type,
        PaymentMethod? method, PaymentStatus? status, string? q)
    {
        var query = _db.Payments.AsNoTracking().AsQueryable();

        if (dateFrom.HasValue)
        {
            var startDate = dateFrom.Value.Date;
            query = query.Where(p => (p.PaidAt ?? p.CreatedAt) >= startDate);
        }
        if (dateTo.HasValue)
        {
            var end = dateTo.Value.Date.AddDays(1);
            query = query.Where(p => (p.PaidAt ?? p.CreatedAt) < end);
        }
        if (type.HasValue) query = query.Where(p => p.Type == type.Value);
        if (method.HasValue) query = query.Where(p => p.Method == method.Value);
        if (status.HasValue) query = query.Where(p => p.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(p => p.ReceiptNo.Contains(q)
                                     || (p.Member != null && (p.Member.FullName.Contains(q)
                                                              || p.Member.MemberCode.Contains(q)
                                                              || p.Member.Phone.Contains(q))));
        }
        return query;
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
