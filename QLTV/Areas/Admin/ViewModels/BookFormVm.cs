using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibraryManagement.Areas.Admin.ViewModels;

// Dùng chung cho thêm và sửa đầu sách (có upload ảnh bìa)
public class BookFormVm
{
    public int Id { get; set; }
    public bool IsEdit => Id > 0;

    [Required(ErrorMessage = "Vui lòng nhập tên sách"), StringLength(250)]
    [Display(Name = "Tên sách")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập ISBN"), StringLength(20)]
    [Display(Name = "ISBN")]
    public string ISBN { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn tác giả")]
    [Display(Name = "Tác giả")]
    public int AuthorId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn thể loại")]
    [Display(Name = "Thể loại")]
    public int CategoryId { get; set; }

    [StringLength(150)]
    [Display(Name = "Nhà xuất bản")]
    public string? Publisher { get; set; }

    [Range(1000, 2100, ErrorMessage = "Năm xuất bản không hợp lệ")]
    [Display(Name = "Năm xuất bản")]
    public int? PublishYear { get; set; }

    [Range(1, 10000, ErrorMessage = "Số trang không hợp lệ")]
    [Display(Name = "Số trang")]
    public int? PageCount { get; set; }

    [Range(0, 100000000, ErrorMessage = "Phí thuê không hợp lệ")]
    [Display(Name = "Phí thuê (đ)")]
    public decimal RentalFee { get; set; }

    [Range(0, 100000000, ErrorMessage = "Giá đền không hợp lệ")]
    [Display(Name = "Giá đền khi mất/hỏng (đ)")]
    public decimal ReplacementPrice { get; set; }

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Display(Name = "Ảnh bìa (jpg, png, webp, tối đa 2MB)")]
    public IFormFile? CoverFile { get; set; }

    public string? ExistingCoverPath { get; set; }

    [Display(Name = "Xóa ảnh bìa hiện tại")]
    public bool RemoveCover { get; set; }

    [Display(Name = "Đang kinh doanh")]
    public bool IsActive { get; set; } = true;

    // Chỉ dùng khi thêm mới: tạo luôn một số bản sao
    [Range(0, 50, ErrorMessage = "Số bản sao từ 0 đến 50")]
    [Display(Name = "Số bản sao ban đầu")]
    public int InitialCopies { get; set; } = 1;

    [StringLength(50)]
    [Display(Name = "Vị trí kệ")]
    public string? ShelfLocation { get; set; }

    [ValidateNever]
    public IEnumerable<SelectListItem> Authors { get; set; } = new List<SelectListItem>();

    [ValidateNever]
    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
}
