using System.Linq.Expressions;
using LibraryManagement.Models;
using LibraryManagement.Models.Enums;
using LibraryManagement.ViewModels;

namespace LibraryManagement.Services;

public static class BookQueries
{
    // Chuyển Book thành thẻ sách hiển thị (tính sẵn số bản còn mượn được)
    public static readonly Expression<Func<Book, BookCardVm>> ToCard = b => new BookCardVm
    {
        Id = b.Id,
        Title = b.Title,
        Author = b.Author!.Name,
        Category = b.Category!.Name,
        CoverImagePath = b.CoverImagePath,
        RentalFee = b.RentalFee,
        Borrowable = b.Copies.Count(c => c.Status == CopyStatus.Available && !c.ReadOnSiteOnly),
        ReadOnSite = b.Copies.Count(c => c.Status == CopyStatus.Available && c.ReadOnSiteOnly),
        TotalCopies = b.Copies.Count(c => c.Status != CopyStatus.Disposed)
    };

    // Sách đang kinh doanh và còn ít nhất một bản trong kho
    public static IQueryable<Book> InStock(this IQueryable<Book> books) =>
        books.Where(b => b.IsActive && b.Copies.Any(c => c.Status != CopyStatus.Disposed));

    // Một nơi duy nhất cho logic tìm kiếm, dùng chung cho trang web và REST API
    public static IQueryable<Book> Search(this IQueryable<Book> query, string? q, int? categoryId,
        bool availableOnly, string? sort)
    {
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(b => b.Title.Contains(q) || b.ISBN.Contains(q) || b.Author!.Name.Contains(q));
        }
        if (categoryId.HasValue)
            query = query.Where(b => b.CategoryId == categoryId.Value);
        if (availableOnly)
            query = query.Where(b => b.Copies.Any(c => c.Status == CopyStatus.Available && !c.ReadOnSiteOnly));

        return sort == "new" ? query.OrderByDescending(b => b.Id) : query.OrderBy(b => b.Title);
    }
}
