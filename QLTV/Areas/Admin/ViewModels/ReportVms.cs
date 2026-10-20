namespace LibraryManagement.Areas.Admin.ViewModels;

public class RevenueLine
{
    public string Label { get; set; } = string.Empty;
    public decimal MembershipFee { get; set; }
    public decimal RentalFee { get; set; }
    public decimal LateFine { get; set; }
    public decimal Compensation { get; set; }
    public decimal Total => MembershipFee + RentalFee + LateFine + Compensation;
}

public class RevenueReportVm
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public string GroupBy { get; set; } = "day";
    public List<RevenueLine> Lines { get; set; } = new();
    public decimal MembershipFee { get; set; }
    public decimal RentalFee { get; set; }
    public decimal LateFine { get; set; }
    public decimal Compensation { get; set; }
    public decimal Total => MembershipFee + RentalFee + LateFine + Compensation;
    public decimal Cash { get; set; }
    public decimal Transfer { get; set; }
    public decimal DepositNet { get; set; }
    public decimal UnpaidTotal { get; set; }
    public decimal MaxLine { get; set; }
}

public class OverdueRow
{
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string MemberCode { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string BookTitle { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public int ItemId { get; set; }
    public DateTime DueDate { get; set; }
    public int DaysLate { get; set; }
    public decimal EstimatedFine { get; set; }
}

public class OverdueReportVm
{
    public List<OverdueRow> Rows { get; set; } = new();
    public decimal TotalFine => Rows.Sum(r => r.EstimatedFine);
    public decimal LateFeePerDay { get; set; }
}

public class TopBookRow
{
    public int Rank { get; set; }
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int LoanCount { get; set; }
}

public class IdleBookRow
{
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int Copies { get; set; }
    public DateTime? LastLoan { get; set; }
}

public class TopBooksVm
{
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int IdleDays { get; set; }
    public List<TopBookRow> Top { get; set; } = new();
    public List<IdleBookRow> Idle { get; set; } = new();
}

public class ExpiringRow
{
    public int MemberId { get; set; }
    public string MemberCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public DateTime EndDate { get; set; }
}

public class ExpiringVm
{
    public int Days { get; set; }
    public List<ExpiringRow> Expiring { get; set; } = new();
    public List<ExpiringRow> RecentlyExpired { get; set; } = new();
}

public class StatusCount
{
    public string Label { get; set; } = string.Empty;
    public string Badge { get; set; } = "secondary";
    public int Count { get; set; }
    public decimal PurchaseValue { get; set; }
}

public class CategoryCount
{
    public string Name { get; set; } = string.Empty;
    public int Titles { get; set; }
    public int Copies { get; set; }
}

public class InventoryVm
{
    public List<StatusCount> Statuses { get; set; } = new();
    public int TotalCopies => Statuses.Sum(s => s.Count);
    public int LostDamagedCount { get; set; }
    public decimal LostDamagedValue { get; set; }
    public List<CategoryCount> Categories { get; set; } = new();
}
