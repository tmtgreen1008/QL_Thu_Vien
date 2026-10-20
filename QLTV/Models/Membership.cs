using System.ComponentModel.DataAnnotations;
using LibraryManagement.Models.Enums;

namespace LibraryManagement.Models;

// Thẻ thành viên. Mỗi lần mua hoặc gia hạn gói là một dòng mới.
public class Membership
{
    public int Id { get; set; }

    public int MemberId { get; set; }
    public Member? Member { get; set; }

    public int PlanId { get; set; }
    public MembershipPlan? Plan { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày bắt đầu")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày hết hạn")]
    public DateTime EndDate { get; set; }

    // Lưu giá tại thời điểm mua, đổi giá gói sau này không ảnh hưởng thẻ cũ
    [Display(Name = "Phí đã trả")]
    public decimal PricePaid { get; set; }

    [Display(Name = "Tiền cọc")]
    public decimal DepositAmount { get; set; }

    [Display(Name = "Trạng thái cọc")]
    public DepositStatus DepositStatus { get; set; } = DepositStatus.Held;

    [Display(Name = "Trạng thái thẻ")]
    public MembershipStatus Status { get; set; } = MembershipStatus.Active;

    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
