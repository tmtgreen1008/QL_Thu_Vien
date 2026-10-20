using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models;

public class MembershipPlan
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên gói"), StringLength(100)]
    [Display(Name = "Tên gói")]
    public string Name { get; set; } = string.Empty;

    [Range(0, 100000000)]
    [Display(Name = "Phí gói")]
    public decimal Price { get; set; }

    [Range(1, 120)]
    [Display(Name = "Thời hạn (tháng)")]
    public int DurationMonths { get; set; } = 1;

    [Range(0, 100000000)]
    [Display(Name = "Tiền cọc")]
    public decimal DepositAmount { get; set; }

    [Range(0, 100)]
    [Display(Name = "Số sách mượn tối đa")]
    public int MaxBooks { get; set; } = 3;

    [Range(1, 365)]
    [Display(Name = "Số ngày được mượn")]
    public int LoanDays { get; set; } = 14;

    [Range(0, 10)]
    [Display(Name = "Số lần gia hạn")]
    public int MaxRenewals { get; set; } = 1;

    [Display(Name = "Đang bán")]
    public bool IsActive { get; set; } = true;

    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
}
