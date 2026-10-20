using System.ComponentModel.DataAnnotations;
using LibraryManagement.Areas.Admin.Services;
using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _db;
    private readonly Loc _t;

    public RegisterModel(UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager, ApplicationDbContext db, Loc t)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _t = t;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên"), StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email (dùng để đăng nhập)")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^(0|\+84)\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ (ví dụ 0901234567)")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Mã thành viên (nếu đã có thẻ tại quầy)")]
        public string? MemberCode { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu từ 6 ký tự trở lên")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [Display(Name = "Nhập lại mật khẩu")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet(string? returnUrl = null) => ReturnUrl = returnUrl;

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        returnUrl ??= Url.Content("~/My");

        if (!ModelState.IsValid) return Page();

        var phone = Input.Phone.Trim();
        var code = Input.MemberCode?.Trim();
        Member? member = null;

        // Liên kết với hồ sơ đã tạo tại quầy: phải khớp CẢ mã thành viên lẫn số điện thoại,
        // nếu không ai biết số điện thoại của người khác cũng chiếm được hồ sơ và thẻ của họ
        if (!string.IsNullOrEmpty(code))
        {
            member = await _db.Members.FirstOrDefaultAsync(m => m.MemberCode == code);
            if (member == null || member.Phone != phone || member.UserId != null)
            {
                ModelState.AddModelError(string.Empty, _t["reg.err.mismatch"]);
                return Page();
            }
        }
        else if (await _db.Members.AnyAsync(m => m.Phone == phone))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.Phone)}", _t["reg.err.phoneexists"]);
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            EmailConfirmed = true,   // chưa có dịch vụ gửi email xác nhận
            FullName = Input.FullName.Trim()
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        await _userManager.AddToRoleAsync(user, AppRoles.User);

        if (member != null)
        {
            member.UserId = user.Id;
        }
        else
        {
            member = new Member
            {
                UserId = user.Id,
                MemberCode = await CodeGenerator.NextMemberCodeAsync(_db),
                FullName = user.FullName,
                Phone = phone,
                Email = Input.Email
            };
            _db.Members.Add(member);
        }
        await _db.SaveChangesAsync();

        await _signInManager.SignInAsync(user, isPersistent: false);
        return LocalRedirect(returnUrl);
    }
}
