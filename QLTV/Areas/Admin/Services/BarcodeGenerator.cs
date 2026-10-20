using LibraryManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Areas.Admin.Services;

// Sinh mã vạch dạng BC + mã sách (4 số) + số thứ tự bản sao (2 số), ví dụ BC000102
public static class BarcodeGenerator
{
    public static async Task<List<string>> NextAsync(ApplicationDbContext db, int bookId, int count)
    {
        var result = new List<string>();
        int n = await db.BookCopies.CountAsync(c => c.BookId == bookId);

        while (result.Count < count)
        {
            n++;
            var code = $"BC{bookId:D4}{n:D2}";
            if (result.Contains(code) || await db.BookCopies.AnyAsync(c => c.Barcode == code))
                continue;
            result.Add(code);
        }

        return result;
    }
}
