using LibraryManagement.Models;
using LibraryManagement.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibraryManagement.Areas.Admin.ViewModels;

public static class PaymentLabels
{
    public static string Type(PaymentType t) => t switch
    {
        PaymentType.MembershipFee => "Phí thẻ",
        PaymentType.Deposit => "Tiền cọc",
        PaymentType.DepositRefund => "Hoàn cọc",
        PaymentType.RentalFee => "Phí thuê sách",
        PaymentType.LateFine => "Phạt trả trễ",
        PaymentType.Compensation => "Đền sách",
        _ => t.ToString()
    };

    public static string Method(PaymentMethod m) => m switch
    {
        PaymentMethod.Cash => "Tiền mặt",
        PaymentMethod.Transfer => "Chuyển khoản",
        _ => m.ToString()
    };

    public static string Status(PaymentStatus s) => s switch
    {
        PaymentStatus.Paid => "Đã thu",
        PaymentStatus.Unpaid => "Còn nợ",
        PaymentStatus.Voided => "Đã hủy",
        _ => s.ToString()
    };

    public static string StatusBadge(PaymentStatus s) => s switch
    {
        PaymentStatus.Paid => "success",
        PaymentStatus.Unpaid => "warning",
        _ => "secondary"
    };

    public static List<SelectListItem> MethodOptions(PaymentMethod? selected = null) =>
        new[] { PaymentMethod.Cash, PaymentMethod.Transfer }
            .Select(m => new SelectListItem(Method(m), ((int)m).ToString(), m == selected))
            .ToList();
}

public static class MembershipLabels
{
    // Trạng thái của một dòng thẻ, tính từ ngày hôm nay
    public static (string Text, string Badge) Describe(Membership m, DateTime today)
    {
        if (m.Status == MembershipStatus.Cancelled) return ("Đã hủy", "secondary");
        if (m.EndDate.Date < today) return ("Hết hạn", "danger");
        if (m.StartDate.Date > today) return ("Chờ hiệu lực", "info");
        return ("Đang hiệu lực", "success");
    }

    public static string Deposit(DepositStatus s) => s switch
    {
        DepositStatus.Held => "Đang giữ",
        DepositStatus.Refunded => "Đã hoàn",
        DepositStatus.Forfeited => "Mất cọc",
        _ => s.ToString()
    };

    // Dùng cho danh sách khách: hạn thẻ xa nhất
    public static (string Text, string Badge) ValidUntil(DateTime? validUntil, DateTime today)
    {
        if (validUntil == null) return ("Chưa có thẻ", "secondary");
        var d = validUntil.Value.Date;
        if (d < today) return ("Hết hạn " + d.ToString("dd/MM/yyyy"), "danger");
        if (d <= today.AddDays(14)) return ("Sắp hết hạn " + d.ToString("dd/MM/yyyy"), "warning");
        return ("Còn hạn đến " + d.ToString("dd/MM/yyyy"), "success");
    }
}
