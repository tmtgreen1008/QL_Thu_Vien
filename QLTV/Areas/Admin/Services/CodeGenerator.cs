using LibraryManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Areas.Admin.Services;

// Sinh mã thành viên (TV000001) và số biên lai (PT000001) tăng dần
public static class CodeGenerator
{
    public static async Task<string> NextMemberCodeAsync(ApplicationDbContext db)
    {
        var last = await db.Members
            .Where(m => m.MemberCode.StartsWith("TV"))
            .OrderByDescending(m => m.MemberCode)
            .Select(m => m.MemberCode)
            .FirstOrDefaultAsync();
        return "TV" + (ParseNumber(last, 2) + 1).ToString("D6");
    }

    public static async Task<List<string>> NextReceiptNosAsync(ApplicationDbContext db, int count)
    {
        var last = await db.Payments
            .Where(p => p.ReceiptNo.StartsWith("PT"))
            .OrderByDescending(p => p.ReceiptNo)
            .Select(p => p.ReceiptNo)
            .FirstOrDefaultAsync();

        int n = ParseNumber(last, 2);
        var result = new List<string>();
        for (int i = 0; i < count; i++)
            result.Add("PT" + (++n).ToString("D6"));
        return result;
    }

    private static int ParseNumber(string? code, int prefixLength) =>
        code != null && int.TryParse(code.AsSpan(prefixLength), out var n) ? n : 0;
}
