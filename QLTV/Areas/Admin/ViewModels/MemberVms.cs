using System.ComponentModel.DataAnnotations;
using LibraryManagement.Models;
using LibraryManagement.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace LibraryManagement.Areas.Admin.ViewModels;

public class PlanRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMonths { get; set; }
    public decimal DepositAmount { get; set; }
    public int MaxBooks { get; set; }
    public int LoanDays { get; set; }
    public int MaxRenewals { get; set; }
    public bool IsActive { get; set; }
    public int CardCount { get; set; }
}

public class MemberRow
{
    public int Id { get; set; }
    public string MemberCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public DateTime? ValidUntil { get; set; }
}

public class MemberIndexVm
{
    public PagedResult<MemberRow> Result { get; set; } = new();
    public string? Search { get; set; }
    public string? Filter { get; set; }
}

public class MemberFormVm
{
    public int Id { get; set; }
    public bool IsEdit => Id > 0;
    public string? MemberCode { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên"), StringLength(100)]
    [Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại"), StringLength(15)]
    [RegularExpression(@"^(0|\+84)\d{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ (ví dụ 0901234567)")]
    [Display(Name = "Số điện thoại")]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ"), StringLength(100)]
    public string? Email { get; set; }

    [StringLength(250)]
    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày sinh")]
    public DateTime? DateOfBirth { get; set; }
}

public class MemberDetailsVm
{
    public Member Member { get; set; } = null!;
    public List<Membership> Memberships { get; set; } = new();
    public Membership? CurrentCard { get; set; }
    public decimal HeldDeposit { get; set; }
    public int OpenLoanItems { get; set; }
    public decimal UnpaidTotal { get; set; }
    public List<Payment> RecentPayments { get; set; } = new();
    public List<Payment> UnpaidPayments { get; set; } = new();
}

public class SellMembershipVm
{
    public int MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberCode { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn gói thành viên")]
    public int PlanId { get; set; }

    [Display(Name = "Hình thức thanh toán")]
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    [StringLength(250)]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    public bool NeedsDeposit { get; set; }
    public bool IsRenewal { get; set; }
    public DateTime StartDate { get; set; }

    [ValidateNever]
    public List<MembershipPlan> Plans { get; set; } = new();
}

public class RefundVm
{
    public int MemberId { get; set; }
    public string? MemberName { get; set; }
    public string? MemberCode { get; set; }
    public decimal HeldDeposit { get; set; }
    public int ActiveCards { get; set; }
    public int OpenLoanItems { get; set; }
    public decimal UnpaidTotal { get; set; }
    public bool CanProceed => OpenLoanItems == 0 && UnpaidTotal == 0 && (ActiveCards > 0 || HeldDeposit > 0);
}
