using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models;

// Phiếu mượn: một lần khách mượn sách (có thể nhiều cuốn).
public class Loan
{
    public int Id { get; set; }

    public int MemberId { get; set; }
    public Member? Member { get; set; }

    public int MembershipId { get; set; }
    public Membership? Membership { get; set; }

    // Admin lập phiếu
    public string AdminId { get; set; } = string.Empty;
    public ApplicationUser? Admin { get; set; }

    [Display(Name = "Ngày mượn")]
    public DateTime LoanDate { get; set; } = DateTime.Now;

    [StringLength(250)]
    [Display(Name = "Ghi chú")]
    public string? Note { get; set; }

    public ICollection<LoanItem> Items { get; set; } = new List<LoanItem>();
}
