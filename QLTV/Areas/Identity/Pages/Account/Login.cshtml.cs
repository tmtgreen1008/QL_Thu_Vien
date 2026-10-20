using System.ComponentModel.DataAnnotations;
using LibraryManagement.Models;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LibraryManagement.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly Loc _t;

    public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, Loc t)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _t = t;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }

    public void OnGet(string? returnUrl = null) => ReturnUrl = returnUrl;

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        if (!ModelState.IsValid) return Page();

        // lockoutOnFailure: true => sai mật khẩu nhiều lần thì tài khoản bị khóa tạm thời (chống dò mật khẩu)
        var result = await _signInManager.PasswordSignInAsync(
            Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(Input.Email);

            // Tài khoản bị vô hiệu hóa (IsActive = false) thì không cho vào dù đúng mật khẩu
            if (user is { IsActive: false })
            {
                await _signInManager.SignOutAsync();
                ModelState.AddModelError(string.Empty, _t["login.err.disabled"]);
                return Page();
            }

            return LocalRedirect(await LandingAsync(user, returnUrl));
        }

        if (result.IsLockedOut)
        {
            var minutes = (int)Math.Ceiling(_userManager.Options.Lockout.DefaultLockoutTimeSpan.TotalMinutes);
            ModelState.AddModelError(string.Empty, _t["login.err.locked", minutes]);
        }
        else
        {
            // Một thông báo chung cho cả "sai email" và "sai mật khẩu" để không lộ email nào đã đăng ký
            ModelState.AddModelError(string.Empty, _t["login.err.invalid"]);
        }
        return Page();
    }

    // Admin vào khu quản trị, khách hàng vào "Tài khoản của tôi"
    private async Task<string> LandingAsync(ApplicationUser? user, string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return returnUrl;
        if (user != null && await _userManager.IsInRoleAsync(user, AppRoles.Admin)) return "/Admin";
        if (user != null && await _userManager.IsInRoleAsync(user, AppRoles.User)) return "/My";
        return "/";
    }
}
