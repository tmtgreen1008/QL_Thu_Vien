using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models;

public class Category
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên thể loại"), StringLength(100)]
    [Display(Name = "Tên thể loại")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    public ICollection<Book> Books { get; set; } = new List<Book>();
}
