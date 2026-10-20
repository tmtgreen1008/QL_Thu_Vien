using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Areas.Admin.Services;

// Kết quả kiểm tra khách có được mượn sách hay không
public class BorrowEligibility
{
    public Member Member { get; init; } = null!;
    public Membership? Card { get; init; }
    public int OpenCount { get; init; }
    public int OverdueCount { get; init; }
    public decimal UnpaidTotal { get; init; }
    public List<string> Blockers { get; } = new();

    public MembershipPlan? Plan => Card?.Plan;
    public int MaxBooks => Plan?.MaxBooks ?? 0;
    public int Remaining => Math.Max(0, MaxBooks - OpenCount);
    public bool CanBorrow => Blockers.Count == 0;
}

public class RenewDecision
{
    public string? Error { get; init; }
    public string? Code { get; init; }   // mã lý do để giao diện khách hàng tự dịch
    public int Max { get; init; }
    public int AddDays { get; init; }
}

// Toàn bộ quy tắc mượn / gia hạn nằm ở một chỗ, để cổng khách hàng dùng lại sau này
public static class LoanRules
{
    public static async Task<BorrowEligibility> CheckAsync(ApplicationDbContext db, int memberId, DateTime today)
    {
        var member = await db.Members.AsNoTracking().FirstAsync(m => m.Id == memberId);

        // Thẻ đang hiệu lực: chưa hủy và hôm nay nằm trong khoảng StartDate - EndDate
        var card = await db.Memberships.AsNoTracking().Include(m => m.Plan)
            .Where(m => m.MemberId == memberId
                        && m.Status == MembershipStatus.Active
                        && m.StartDate <= today && m.EndDate >= today)
            .OrderByDescending(m => m.StartDate)
            .FirstOrDefaultAsync();

        int open = await db.LoanItems.CountAsync(i => i.Loan!.MemberId == memberId && i.ReturnedAt == null);
        int overdue = await db.LoanItems.CountAsync(i =>
            i.Loan!.MemberId == memberId && i.ReturnedAt == null && i.DueDate < today);
        decimal unpaid = await db.Payments
            .Where(p => p.MemberId == memberId && p.Status == PaymentStatus.Unpaid)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        var e = new BorrowEligibility
        {
            Member = member,
            Card = card,
            OpenCount = open,
            OverdueCount = overdue,
            UnpaidTotal = unpaid
        };

        if (member.IsLocked)
            e.Blockers.Add("Khách đang bị khóa" +
                           (string.IsNullOrWhiteSpace(member.LockReason) ? "." : $": {member.LockReason}"));

        if (card == null)
            e.Blockers.Add("Khách chưa có thẻ còn hiệu lực, cần bán hoặc gia hạn thẻ.");
        else if (card.Plan!.MaxBooks <= 0)
            e.Blockers.Add($"Gói \"{card.Plan.Name}\" chỉ đọc tại chỗ, không được mượn về.");
        else if (open >= card.Plan.MaxBooks)
            e.Blockers.Add($"Khách đã mượn đủ {card.Plan.MaxBooks} cuốn (tối đa của gói).");

        if (overdue > 0)
            e.Blockers.Add($"Khách còn {overdue} cuốn quá hạn, cần trả trước khi mượn tiếp.");

        if (unpaid > 0)
            e.Blockers.Add($"Khách còn nợ {unpaid:N0} đ, cần thanh toán trước khi mượn tiếp.");

        return e;
    }

    // Gia hạn thêm đúng số ngày mượn của gói, tính từ hạn trả cũ
    public static async Task<RenewDecision> CheckRenewAsync(ApplicationDbContext db, LoanItem item, DateTime today)
    {
        if (item.ReturnedAt != null || item.Status != LoanItemStatus.Borrowed)
            return new RenewDecision { Error = "Sách này đã được trả.", Code = "returned" };

        var e = await CheckAsync(db, item.Loan!.MemberId, today);

        if (e.Member.IsLocked)
            return new RenewDecision { Error = "Khách đang bị khóa.", Code = "locked" };
        if (e.Card == null)
            return new RenewDecision { Error = "Khách chưa có thẻ còn hiệu lực.", Code = "nocard" };
        if (item.DueDate.Date < today)
            return new RenewDecision { Error = "Sách đã quá hạn, không gia hạn được. Hãy trả sách.", Code = "item_overdue" };
        if (e.OverdueCount > 0)
            return new RenewDecision { Error = "Khách còn sách quá hạn khác.", Code = "other_overdue" };
        if (item.RenewCount >= e.Card.Plan!.MaxRenewals)
            return new RenewDecision { Error = $"Đã gia hạn tối đa {e.Card.Plan.MaxRenewals} lần.", Code = "max_renewals", Max = e.Card.Plan.MaxRenewals };

        return new RenewDecision { AddDays = e.Card.Plan.LoanDays };
    }
}
