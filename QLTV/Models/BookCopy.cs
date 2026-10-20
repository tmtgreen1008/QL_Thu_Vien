using System.ComponentModel.DataAnnotations;
using LibraryManagement.Models.Enums;

namespace LibraryManagement.Models;

// Một cuốn sách vật lý, có mã vạch riêng.
public class BookCopy
{
    public int Id { get; set; }

    public int BookId { get; set; }
    public Book? Book { get; set; }

    [Required, StringLength(30)]
    [Display(Name = "Mã vạch")]
    public string Barcode { get; set; } = string.Empty;

    [Display(Name = "Trạng thái")]
    public CopyStatus Status { get; set; } = CopyStatus.Available;

    [Display(Name = "Tình trạng sách")]
    public CopyCondition Condition { get; set; } = CopyCondition.New;

    [StringLength(50)]
    [Display(Name = "Vị trí kệ")]
    public string? ShelfLocation { get; set; }

    [Display(Name = "Chỉ đọc tại chỗ")]
    public bool ReadOnSiteOnly { get; set; }

    [Display(Name = "Giá nhập")]
    public decimal PurchasePrice { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày nhập")]
    public DateTime AcquiredDate { get; set; } = DateTime.Today;

    // Chống hai Admin cùng cho mượn một bản sách (kiểm soát đồng thời)
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<LoanItem> LoanItems { get; set; } = new List<LoanItem>();
}
