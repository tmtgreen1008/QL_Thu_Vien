using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;

namespace LibraryManagement.Services;

// SESSION: "Giỏ sách quan tâm" của người đang truy cập (mất khi hết phiên)
public static class WishlistSession
{
    private const string Key = "wishlist";
    public const int MaxItems = 20;

    public static List<int> Get(HttpContext ctx)
    {
        var session = ctx.Features.Get<ISessionFeature>()?.Session;
        if (session == null) return new List<int>();

        var json = session.GetString(Key);
        if (string.IsNullOrEmpty(json)) return new List<int>();
        try { return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>(); }
        catch (JsonException) { return new List<int>(); }
    }

    public static void Save(HttpContext ctx, List<int> ids)
    {
        var session = ctx.Features.Get<ISessionFeature>()?.Session;
        session?.SetString(Key, JsonSerializer.Serialize(ids));
    }

    // Dùng được trong layout kể cả khi chưa bật session
    public static int Count(HttpContext ctx) => Get(ctx).Count;
}

// COOKIE: danh sách sách xem gần đây, lưu 30 ngày trên trình duyệt
public static class RecentCookie
{
    private const string Name = "recent_books";
    private const int Max = 6;

    public static List<int> Get(HttpRequest request)
    {
        if (!request.Cookies.TryGetValue(Name, out var raw) || string.IsNullOrWhiteSpace(raw))
            return new List<int>();

        // Cookie do người dùng sửa được nên chỉ nhận số nguyên dương
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var n) ? n : 0)
            .Where(n => n > 0)
            .Distinct()
            .Take(Max)
            .ToList();
    }

    public static void Push(HttpContext ctx, int bookId)
    {
        var ids = Get(ctx.Request);
        ids.Remove(bookId);
        ids.Insert(0, bookId);

        ctx.Response.Cookies.Append(Name, string.Join(",", ids.Take(Max)), new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = ctx.Request.IsHttps,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });
    }
}
