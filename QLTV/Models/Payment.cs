using System.ComponentModel.DataAnnotations;
using LibraryManagement.Models.Enums;

namespace LibraryManagement.Models;

// Sổ giao dịch. Không xóa dòng, chỉ hủy (Voided). Hoàn cọc là số tiền âm.
// Status = Unpaid dùng cho khoản còn nợ (phạt, đền sách chưa thu được).
public class Payment
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    [Display(Name = "Số biên lai")]
    public string ReceiptNo { get; set; } = string.Empty;

    [Display(Name = "Loại khoản thu")]
    public PaymentType Type { get; set; }

    [Display(Name = "Số tiền")]
    public decimal Amount { get; set; }

    [Display(Name = "Hình thức")]
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    [Display(Name = "Trạng thái")]
    public PaymentStatus Status { get; set; } = PaymentStatus.Paid;

    public int? MemberId { get; set; }
    public Member? Member { get; set; }

    public int? MembershipId { get; set; }
    public Membership? Membership { get; set; }

    public int? LoanItemId { get; set; }
    public LoanItem? LoanItem { get; set; }

    // Admin thu tiền
    public string ReceivedById { get; set; } = string.Empty;
    public ApplicationUser? ReceivedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Display(Name = "Ngày thu")]
    public DateTime? PaidAt { get; set; }

    [StringLength(250)]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    [StringLength(250)]
    [Display(Name = "Lý do hủy")]
    public string? VoidReason { get; set; }
}
