using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibraryManagement.Areas.Admin.ViewModels;

public class BookRow
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal RentalFee { get; set; }
    public bool IsActive { get; set; }
    public string? CoverImagePath { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
}

public class BookIndexVm
{
    public PagedResult<BookRow> Result { get; set; } = new();
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
}
