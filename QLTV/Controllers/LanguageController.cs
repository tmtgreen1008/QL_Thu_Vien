using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace QLTV.Controllers;

// Đổi ngôn ngữ: lưu lựa chọn vào COOKIE, RequestLocalizationMiddleware đọc cookie ở các request sau
public class LanguageController : Controller
{
    private static readonly string[] Supported = { "vi", "en" };

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Set(string culture, string? returnUrl)
    {
        if (!Supported.Contains(culture)) culture = "vi";

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps
            });

        // Chỉ chuyển hướng tới địa chỉ nội bộ (chống open redirect)
        return LocalRedirect(!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
    }
}
