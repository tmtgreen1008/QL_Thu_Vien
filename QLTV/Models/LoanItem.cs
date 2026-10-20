using System.ComponentModel.DataAnnotations;
using LibraryManagement.Models.Enums;

namespace LibraryManagement.Models;

// Từng cuốn trong phiếu mượn. "Quá hạn" không lưu, tính từ DueDate và ReturnedAt.
public class LoanItem
{
    public int Id { get; set; }

    public int LoanId { get; set; }
    public Loan? Loan { get; set; }

    public int BookCopyId { get; set; }
    public BookCopy? BookCopy { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Hạn trả")]
    public DateTime DueDate { get; set; }

    [Display(Name = "Số lần gia hạn")]
    public int RenewCount { get; set; }

    // Lưu phí thuê tại thời điểm mượn
    [Display(Name = "Phí thuê")]
    public decimal RentalFeeCharged { get; set; }

    // null = chưa trả
    [Display(Name = "Ngày trả")]
    public DateTime? ReturnedAt { get; set; }

    [Display(Name = "Tình trạng khi trả")]
    public CopyCondition? ConditionOnReturn { get; set; }

    [Display(Name = "Trạng thái")]
    public LoanItemStatus Status { get; set; } = LoanItemStatus.Borrowed;

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
