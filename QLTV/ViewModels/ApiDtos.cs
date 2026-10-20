namespace LibraryManagement.ViewModels;

// Các đối tượng trả về của REST API (không lộ entity ra ngoài)
public class BookDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? CoverImagePath { get; set; }
    public decimal RentalFee { get; set; }
    public int Borrowable { get; set; }
    public int ReadOnSite { get; set; }
    public int TotalCopies { get; set; }
    public string Url { get; set; } = string.Empty;

    public static BookDto From(BookCardVm c) => new()
    {
        Id = c.Id, Title = c.Title, Author = c.Author, Category = c.Category,
        CoverImagePath = c.CoverImagePath, RentalFee = c.RentalFee,
        Borrowable = c.Borrowable, ReadOnSite = c.ReadOnSite, TotalCopies = c.TotalCopies,
        Url = $"/Catalog/Details/{c.Id}"
    };
}

public class BookDetailDto : BookDto
{
    public string Isbn { get; set; } = string.Empty;
    public string? Publisher { get; set; }
    public int? PublishYear { get; set; }
    public int? PageCount { get; set; }
    public string? Description { get; set; }
}

public class PagedApiResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Titles { get; set; }
}

public class LoanItemDto
{
    public int ItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public DateOnly LoanDate { get; set; }
    public DateOnly DueDate { get; set; }
    public int DaysLeft { get; set; }
    public int RenewCount { get; set; }
    public bool CanRenew { get; set; }
}

public class RenewResultDto
{
    public int ItemId { get; set; }
    public DateOnly DueDate { get; set; }
    public int RenewCount { get; set; }
    public int MaxRenewals { get; set; }
    public string Message { get; set; } = string.Empty;
}
