using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace LibraryManagement.Models;

public class ApplicationUser : IdentityUser
{
    [Required, StringLength(100)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Hồ sơ khách gắn với tài khoản này (null nếu là Admin hoặc chưa liên kết)
    public Member? Member { get; set; }
}
