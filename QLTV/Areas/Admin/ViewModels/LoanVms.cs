using System.ComponentModel.DataAnnotations;
using LibraryManagement.Areas.Admin.Services;
using LibraryManagement.Models.Enums;

namespace LibraryManagement.Areas.Admin.ViewModels;

public class LoanRow
{
    public int Id { get; set; }
    public DateTime LoanDate { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string MemberCode { get; set; } = string.Empty;
    public string AdminName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public int OpenCount { get; set; }
    public int OverdueCount { get; set; }
}

public class LoanIndexVm
{
    public PagedResult<LoanRow> Result { get; set; } = new();
    public string? Search { get; set; }
}

public class MemberMatch
{
    public int Id { get; set; }
    public string MemberCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public class LoanCreateVm
{
    public BorrowEligibility? Eligibility { get; set; }
    public List<MemberMatch> Matches { get; set; } = new();
    public string? Q { get; set; }
    public string? Barcodes { get; set; }
    public string? Note { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class OpenItemRow
{
    public int ItemId { get; set; }
    public int LoanId { get; set; }
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string MemberCode { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string BookTitle { get; set; } = string.Empty;
    public DateTime LoanDate { get; set; }
    public DateTime DueDate { get; set; }
    public int RenewCount { get; set; }
}

public class ReturnIndexVm
{
    public PagedResult<OpenItemRow> Result { get; set; } = new();
    public string? Q { get; set; }
    public bool OverdueOnly { get; set; }
    public int OverdueTotal { get; set; }
}

public enum ReturnOutcome { Normal = 0, Damaged = 1, Lost = 2 }

public class ReturnVm
{
    public int LoanItemId { get; set; }

    // Thông tin hiển thị, do server điền (không tin dữ liệu từ form)
    public int LoanId { get; set; }
    public int MemberId { get; set; }
    public string? BookTitle { get; set; }
    public string? Barcode { get; set; }
    public string? MemberName { get; set; }
    public string? MemberCode { get; set; }
    public DateTime LoanDate { get; set; }
    public DateTime DueDate { get; set; }
    public int DaysLate { get; set; }
    public decimal RentalFee { get; set; }
    public decimal LateFee { get; set; }
    public decimal LateFeePerDay { get; set; }
    public decimal ReplacementPrice { get; set; }

    // Dữ liệu Admin nhập
    public ReturnOutcome Outcome { get; set; } = ReturnOutcome.Normal;

    [Display(Name = "Tình trạng sách khi trả")]
    public CopyCondition Condition { get; set; } = CopyCondition.Good;

    [Range(0, 100000000, ErrorMessage = "Tiền đền không hợp lệ")]
    [Display(Name = "Tiền đền (đ)")]
    public decimal CompensationAmount { get; set; }

    [Display(Name = "Thu tiền ngay")]
    public bool CollectNow { get; set; } = true;

    [Display(Name = "Hình thức thanh toán")]
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
}
