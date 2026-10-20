using LibraryManagement.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibraryManagement.Areas.Admin.ViewModels;

public class PaymentRow
{
    public int Id { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public DateTime When { get; set; }
    public int? MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberCode { get; set; }
    public PaymentType Type { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public string? ReceivedByName { get; set; }
    public string? Note { get; set; }
    public string? VoidReason { get; set; }
}

public class PaymentIndexVm
{
    public PagedResult<PaymentRow> Result { get; set; } = new();
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public PaymentType? Type { get; set; }
    public PaymentMethod? Method { get; set; }
    public PaymentStatus? Status { get; set; }
    public string? Q { get; set; }

    // Tổng của toàn bộ kết quả lọc (không chỉ trang hiện tại)
    public decimal Revenue { get; set; }
    public decimal DepositNet { get; set; }
    public decimal UnpaidTotal { get; set; }
    public int VoidedCount { get; set; }
}

public static class PaymentFilterOptions
{
    public static List<SelectListItem> Types(PaymentType? selected) =>
        Enum.GetValues<PaymentType>()
            .Select(t => new SelectListItem(PaymentLabels.Type(t), ((int)t).ToString(), t == selected)).ToList();

    public static List<SelectListItem> Methods(PaymentMethod? selected) =>
        Enum.GetValues<PaymentMethod>()
            .Select(m => new SelectListItem(PaymentLabels.Method(m), ((int)m).ToString(), m == selected)).ToList();

    public static List<SelectListItem> Statuses(PaymentStatus? selected) =>
        Enum.GetValues<PaymentStatus>()
            .Select(s => new SelectListItem(PaymentLabels.Status(s), ((int)s).ToString(), s == selected)).ToList();
}
