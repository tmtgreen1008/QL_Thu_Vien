using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models;

// Hồ sơ khách hàng. UserId null khi khách mới đến quầy, chưa có tài khoản web.
public class Member
{
    public int Id { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [Required, StringLength(20)]
    [Display(Name = "Mã thành viên")]
    public string MemberCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên"), StringLength(100)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại"), StringLength(15)]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [Display(Name = "Số điện thoại")]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress, StringLength(100)]
    public string? Email { get; set; }

    [StringLength(250)]
    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày sinh")]
    public DateTime? DateOfBirth { get; set; }

    [Display(Name = "Đã khóa")]
    public bool IsLocked { get; set; }

    [StringLength(250)]
    [Display(Name = "Lý do khóa")]
    public string? LockReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
