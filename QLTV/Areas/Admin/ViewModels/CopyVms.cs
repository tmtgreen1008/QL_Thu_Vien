using System.ComponentModel.DataAnnotations;
using LibraryManagement.Models.Enums;

namespace LibraryManagement.Areas.Admin.ViewModels;

public class AddCopiesVm
{
    public int BookId { get; set; }
    public string? BookTitle { get; set; }

    [Range(1, 50, ErrorMessage = "Số lượng từ 1 đến 50")]
    [Display(Name = "Số bản sao cần thêm")]
    public int Quantity { get; set; } = 1;

    [StringLength(50)]
    [Display(Name = "Vị trí kệ")]
    public string? ShelfLocation { get; set; }

    [Display(Name = "Tình trạng sách")]
    public CopyCondition Condition { get; set; } = CopyCondition.New;

    [Range(0, 100000000, ErrorMessage = "Giá nhập không hợp lệ")]
    [Display(Name = "Giá nhập mỗi bản (đ)")]
    public decimal PurchasePrice { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày nhập")]
    public DateTime AcquiredDate { get; set; } = DateTime.Today;

    [Display(Name = "Chỉ đọc tại chỗ (không cho mượn về)")]
    public bool ReadOnSiteOnly { get; set; }
}

public class EditCopyVm
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string? BookTitle { get; set; }
    public string? Barcode { get; set; }
    public bool IsOnLoan { get; set; }

    [Display(Name = "Trạng thái")]
    public CopyStatus Status { get; set; }

    [Display(Name = "Tình trạng sách")]
    public CopyCondition Condition { get; set; }

    [StringLength(50)]
    [Display(Name = "Vị trí kệ")]
    public string? ShelfLocation { get; set; }

    [Display(Name = "Chỉ đọc tại chỗ (không cho mượn về)")]
    public bool ReadOnSiteOnly { get; set; }

    [Range(0, 100000000, ErrorMessage = "Giá nhập không hợp lệ")]
    [Display(Name = "Giá nhập (đ)")]
    public decimal PurchasePrice { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày nhập")]
    public DateTime AcquiredDate { get; set; } = DateTime.Today;
}
