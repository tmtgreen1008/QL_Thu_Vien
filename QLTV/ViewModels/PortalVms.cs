using System.ComponentModel.DataAnnotations;
using LibraryManagement.Areas.Admin.Services;
using LibraryManagement.Areas.Admin.ViewModels;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibraryManagement.ViewModels;

public class BookCardVm
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
}

public class CategoryCardVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Titles { get; set; }
}

public class HomeVm
{
    public int Titles { get; set; }
    public int Copies { get; set; }
    public List<BookCardVm> Latest { get; set; } = new();
    public List<BookCardVm> Recent { get; set; } = new();
    public List<CategoryCardVm> Categories { get; set; } = new();
}

public class CatalogIndexVm
{
    public PagedResult<BookCardVm> Result { get; set; } = new();
    public string? Q { get; set; }
    public int? CategoryId { get; set; }
    public bool AvailableOnly { get; set; }
    public string Sort { get; set; } = "title";
    public List<SelectListItem> Categories { get; set; } = new();
}

public class CatalogDetailsVm
{
    public Book Book { get; set; } = null!;
    public int Borrowable { get; set; }
    public int ReadOnSite { get; set; }
    public int TotalCopies { get; set; }
    public bool InWishlist { get; set; }
    public List<BookCardVm> Related { get; set; } = new();
}

public class WishlistVm
{
    public List<BookCardVm> Items { get; set; } = new();
}

public class PricingVm
{
    public List<MembershipPlan> Plans { get; set; } = new();
    public decimal LateFeePerDay { get; set; }
}

public class MyDashboardVm
{
    public Member Member { get; set; } = null!;
    public BorrowEligibility Eligibility { get; set; } = null!;
    public decimal HeldDeposit { get; set; }
    public DateTime? ValidUntil { get; set; }
    public int DueSoon { get; set; }
}

public class MyLoanRow
{
    public int ItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public DateTime LoanDate { get; set; }
    public DateTime DueDate { get; set; }
    public int RenewCount { get; set; }
    public decimal RentalFee { get; set; }
}

public class MyLoansVm
{
    public List<MyLoanRow> Items { get; set; } = new();
    public int MaxRenewals { get; set; }
    public bool HasValidCard { get; set; }
}

public class MyHistoryRow
{
    public string Title { get; set; } = string.Empty;
    public DateTime LoanDate { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public bool Lost { get; set; }
}

public class MyHistoryVm
{
    public PagedResult<MyHistoryRow> Result { get; set; } = new();
}

public class MyPaymentsVm
{
    public PagedResult<PaymentRow> Result { get; set; } = new();
    public decimal UnpaidTotal { get; set; }
}

public class MyProfileVm
{
    public string? MemberCode { get; set; }
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên"), StringLength(100)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ"), StringLength(100)]
    [Display(Name = "Email liên hệ")]
    public string? Email { get; set; }

    [StringLength(250)]
    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày sinh")]
    public DateTime? DateOfBirth { get; set; }
}
