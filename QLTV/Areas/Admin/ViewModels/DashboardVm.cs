namespace LibraryManagement.Areas.Admin.ViewModels;

public class DashboardVm
{
    public int BookTitles { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public int Members { get; set; }
    public int BorrowedItems { get; set; }
    public int OverdueItems { get; set; }
    public decimal RevenueToday { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public decimal HeldDeposits { get; set; }
}
