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
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public ReportsController(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public IActionResult Index() => RedirectToAction(nameof(Revenue));

    // ---------- 1. Doanh thu theo ngày hoặc tháng ----------
    public async Task<IActionResult> Revenue(DateTime? dateFrom, DateTime? dateTo, string? groupBy)
    {
        var today = DateTime.Today;
        var (startDate, endDate) = NormalizeRange(dateFrom, dateTo, new DateTime(today.Year, today.Month, 1), today);
        bool byMonth = groupBy == "month";
        var end = endDate.AddDays(1);
        var revenueTypes = PaymentRules.RevenueTypes;
        var depositTypes = PaymentRules.DepositTypes;

        var rows = await _db.Payments.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Paid
                        && (p.PaidAt ?? p.CreatedAt) >= startDate && (p.PaidAt ?? p.CreatedAt) < end)
            .Select(p => new { When = p.PaidAt ?? p.CreatedAt, p.Type, p.Amount, p.Method })
            .ToListAsync();

        var revenueRows = rows.Where(r => revenueTypes.Contains(r.Type)).ToList();

        // Dựng đủ các kỳ trong khoảng (kể cả kỳ không có doanh thu)
        var lines = new List<RevenueLine>();
        if (byMonth)
        {
            for (var m = new DateTime(startDate.Year, startDate.Month, 1); m <= endDate && lines.Count < 60; m = m.AddMonths(1))
                lines.Add(BuildLine(m.ToString("MM/yyyy"), revenueRows.Where(r => r.When.Year == m.Year && r.When.Month == m.Month)
                    .Select(r => (r.Type, r.Amount))));
        }
        else
        {
            for (var d = startDate; d <= endDate && lines.Count < 366; d = d.AddDays(1))
                lines.Add(BuildLine(d.ToString("dd/MM/yyyy"), revenueRows.Where(r => r.When.Date == d)
                    .Select(r => (r.Type, r.Amount))));
        }

        var vm = new RevenueReportVm
        {
            DateFrom = startDate,
            DateTo = endDate,
            GroupBy = byMonth ? "month" : "day",
            Lines = lines,
            MembershipFee = lines.Sum(l => l.MembershipFee),
            RentalFee = lines.Sum(l => l.RentalFee),
            LateFine = lines.Sum(l => l.LateFine),
            Compensation = lines.Sum(l => l.Compensation),
            Cash = revenueRows.Where(r => r.Method == PaymentMethod.Cash).Sum(r => r.Amount),
            Transfer = revenueRows.Where(r => r.Method == PaymentMethod.Transfer).Sum(r => r.Amount),
            DepositNet = rows.Where(r => depositTypes.Contains(r.Type)).Sum(r => r.Amount),
            UnpaidTotal = await _db.Payments.Where(p => p.Status == PaymentStatus.Unpaid)
                .SumAsync(p => (decimal?)p.Amount) ?? 0,
            MaxLine = lines.Count == 0 ? 0 : lines.Max(l => l.Total)
        };
        return View(vm);
    }

    // ---------- 2. Sách quá hạn ----------
    public async Task<IActionResult> Overdue()
    {
        var today = DateTime.Today;
        var perDay = _config.GetValue<decimal?>("Library:LateFeePerDay") ?? 2000m;

        var raw = await _db.LoanItems.AsNoTracking()
            .Where(i => i.ReturnedAt == null && i.DueDate < today)
            .OrderBy(i => i.DueDate)
            .Select(i => new
            {
                i.Id,
                MemberId = i.Loan!.MemberId,
                MemberName = i.Loan!.Member!.FullName,
                MemberCode = i.Loan!.Member!.MemberCode,
                Phone = i.Loan!.Member!.Phone,
                Title = i.BookCopy!.Book!.Title,
                Barcode = i.BookCopy!.Barcode,
                Price = i.BookCopy!.Book!.ReplacementPrice,
                i.DueDate
            })
            .ToListAsync();

        var rows = raw.Select(r =>
        {
            int late = Math.Max(0, (today - r.DueDate.Date).Days);
            decimal fine = late * perDay;
            if (r.Price > 0) fine = Math.Min(fine, r.Price);
            return new OverdueRow
            {
                ItemId = r.Id, MemberId = r.MemberId, MemberName = r.MemberName, MemberCode = r.MemberCode,
                Phone = r.Phone, BookTitle = r.Title, Barcode = r.Barcode, DueDate = r.DueDate,
                DaysLate = late, EstimatedFine = fine
            };
        }).ToList();

        return View(new OverdueReportVm { Rows = rows, LateFeePerDay = perDay });
    }

    // ---------- 3. Sách mượn nhiều nhất và sách tồn lâu ----------
    public async Task<IActionResult> TopBooks(DateTime? dateFrom, DateTime? dateTo, int idleDays = 90)
    {
        var today = DateTime.Today;
        var (startDate, endDate) = NormalizeRange(dateFrom, dateTo, today.AddDays(-90), today);
        var end = endDate.AddDays(1);
        idleDays = Math.Clamp(idleDays, 7, 730);

        var counts = await _db.LoanItems.AsNoTracking()
            .Where(i => i.Loan!.LoanDate >= startDate && i.Loan!.LoanDate < end)
            .GroupBy(i => i.BookCopy!.BookId)
            .Select(g => new { BookId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        var ids = counts.Select(c => c.BookId).ToList();
        var titles = await _db.Books.AsNoTracking()
            .Where(b => ids.Contains(b.Id))
            .Select(b => new { b.Id, b.Title, Author = b.Author!.Name })
            .ToDictionaryAsync(b => b.Id);

        var top = counts.Select((c, i) => new TopBookRow
        {
            Rank = i + 1,
            BookId = c.BookId,
            Title = titles[c.BookId].Title,
            Author = titles[c.BookId].Author,
            LoanCount = c.Count
        }).ToList();

        // Sách tồn lâu: còn kinh doanh, có bản sao, lần mượn gần nhất đã quá idleDays (hoặc chưa từng mượn)
        var lastLoans = await _db.LoanItems.AsNoTracking()
            .GroupBy(i => i.BookCopy!.BookId)
            .Select(g => new { BookId = g.Key, Last = g.Max(i => i.Loan!.LoanDate) })
            .ToDictionaryAsync(x => x.BookId, x => x.Last);

        var since = today.AddDays(-idleDays);
        var books = await _db.Books.AsNoTracking()
            .Where(b => b.IsActive && b.Copies.Any(c => c.Status != CopyStatus.Disposed))
            .Select(b => new
            {
                b.Id, b.Title, Author = b.Author!.Name,
                Copies = b.Copies.Count(c => c.Status != CopyStatus.Disposed)
            })
            .ToListAsync();

        var idle = books
            .Select(b => new IdleBookRow
            {
                BookId = b.Id, Title = b.Title, Author = b.Author, Copies = b.Copies,
                LastLoan = lastLoans.TryGetValue(b.Id, out var last) ? last : null
            })
            .Where(b => b.LastLoan == null || b.LastLoan < since)
            .OrderBy(b => b.LastLoan ?? DateTime.MinValue)
            .Take(50)
            .ToList();

        return View(new TopBooksVm { DateFrom = startDate, DateTo = endDate, IdleDays = idleDays, Top = top, Idle = idle });
    }

    // ---------- 4. Thẻ sắp hết hạn ----------
    public async Task<IActionResult> Expiring(int days = 14)
    {
        days = Math.Clamp(days, 1, 90);
        var today = DateTime.Today;
        var soon = today.AddDays(days);
        var past = today.AddDays(-30);

        var all = await _db.Members.AsNoTracking()
            .Select(m => new
            {
                m.Id, m.MemberCode, m.FullName, m.Phone, m.IsLocked,
                MaxEnd = m.Memberships.Where(x => x.Status == MembershipStatus.Active)
                    .Max(x => (DateTime?)x.EndDate)
            })
            .Where(x => x.MaxEnd != null && x.MaxEnd >= past && x.MaxEnd <= soon)
            .ToListAsync();

        var rows = all.Select(x => new ExpiringRow
        {
            MemberId = x.Id, MemberCode = x.MemberCode, FullName = x.FullName, Phone = x.Phone,
            IsLocked = x.IsLocked, EndDate = x.MaxEnd!.Value
        }).ToList();

        return View(new ExpiringVm
        {
            Days = days,
            Expiring = rows.Where(r => r.EndDate.Date >= today).OrderBy(r => r.EndDate).ToList(),
            RecentlyExpired = rows.Where(r => r.EndDate.Date < today).OrderByDescending(r => r.EndDate).ToList()
        });
    }

    // ---------- 5. Kho sách: số bản sao theo trạng thái ----------
    public async Task<IActionResult> Inventory()
    {
        var byStatus = await _db.BookCopies.AsNoTracking()
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key, Count = g.Count(), Value = g.Sum(c => c.PurchasePrice) })
            .ToListAsync();

        var statuses = Enum.GetValues<CopyStatus>().Select(s =>
        {
            var found = byStatus.FirstOrDefault(x => x.Status == s);
            return new StatusCount
            {
                Label = EnumLabels.Status(s),
                Badge = EnumLabels.StatusBadge(s),
                Count = found?.Count ?? 0,
                PurchaseValue = found?.Value ?? 0
            };
        }).ToList();

        var lostDamaged = await _db.BookCopies.AsNoTracking()
            .Where(c => c.Status == CopyStatus.Lost || c.Status == CopyStatus.Damaged)
            .Select(c => c.Book!.ReplacementPrice)
            .ToListAsync();

        var copiesByCat = await _db.BookCopies.AsNoTracking()
            .Where(c => c.Status != CopyStatus.Disposed)
            .GroupBy(c => c.Book!.Category!.Name)
            .Select(g => new { Name = g.Key, Copies = g.Count() })
            .ToListAsync();
        var titlesByCat = await _db.Books.AsNoTracking()
            .Where(b => b.IsActive)
            .GroupBy(b => b.Category!.Name)
            .Select(g => new { Name = g.Key, Titles = g.Count() })
            .ToListAsync();

        var cats = copiesByCat.Select(c => new CategoryCount
        {
            Name = c.Name,
            Copies = c.Copies,
            Titles = titlesByCat.FirstOrDefault(t => t.Name == c.Name)?.Titles ?? 0
        }).OrderByDescending(c => c.Copies).ToList();

        return View(new InventoryVm
        {
            Statuses = statuses,
            LostDamagedCount = lostDamaged.Count,
            LostDamagedValue = lostDamaged.Sum(),
            Categories = cats
        });
    }

    // ---------- Hàm hỗ trợ ----------
    private static (DateTime From, DateTime To) NormalizeRange(DateTime? startDate, DateTime? endDate, DateTime defFrom, DateTime defTo)
    {
        var f = (startDate ?? defFrom).Date;
        var t = (endDate ?? defTo).Date;
        return t < f ? (t, f) : (f, t);
    }

    private static RevenueLine BuildLine(string label, IEnumerable<(PaymentType Type, decimal Amount)> items)
    {
        var list = items.ToList();
        return new RevenueLine
        {
            Label = label,
            MembershipFee = list.Where(x => x.Type == PaymentType.MembershipFee).Sum(x => x.Amount),
            RentalFee = list.Where(x => x.Type == PaymentType.RentalFee).Sum(x => x.Amount),
            LateFine = list.Where(x => x.Type == PaymentType.LateFine).Sum(x => x.Amount),
            Compensation = list.Where(x => x.Type == PaymentType.Compensation).Sum(x => x.Amount)
        };
    }
}
