using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models;

// Đầu sách (thông tin chung). Từng cuốn vật lý nằm ở BookCopy.
public class Book
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên sách"), StringLength(250)]
    [Display(Name = "Tên sách")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập ISBN"), StringLength(20)]
    [Display(Name = "ISBN")]
    public string ISBN { get; set; } = string.Empty;

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [StringLength(150)]
    [Display(Name = "Nhà xuất bản")]
    public string? Publisher { get; set; }

    [Range(1000, 2100, ErrorMessage = "Năm xuất bản không hợp lệ")]
    [Display(Name = "Năm xuất bản")]
    public int? PublishYear { get; set; }

    [Range(1, 10000)]
    [Display(Name = "Số trang")]
    public int? PageCount { get; set; }

    [StringLength(300)]
    [Display(Name = "Ảnh bìa")]
    public string? CoverImagePath { get; set; }

    [Range(0, 100000000)]
    [Display(Name = "Phí thuê")]
    public decimal RentalFee { get; set; }

    [Range(0, 100000000)]
    [Display(Name = "Giá đền (khi mất/hỏng)")]
    public decimal ReplacementPrice { get; set; }

    [Display(Name = "Tác giả")]
    public int AuthorId { get; set; }
    public Author? Author { get; set; }

    [Display(Name = "Thể loại")]
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    [Display(Name = "Đang kinh doanh")]
    public bool IsActive { get; set; } = true;

    public ICollection<BookCopy> Copies { get; set; } = new List<BookCopy>();
}
