namespace LibraryManagement.Areas.Admin.ViewModels;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalItems { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
}

// Dữ liệu cho phần tử phân trang (_Pager.cshtml)
public class PagerVm
{
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public Dictionary<string, string> RouteValues { get; set; } = new();
}
