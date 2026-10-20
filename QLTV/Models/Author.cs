using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models;

public class Author
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên tác giả"), StringLength(150)]
    [Display(Name = "Tên tác giả")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Tiểu sử")]
    public string? Bio { get; set; }

    public ICollection<Book> Books { get; set; } = new List<Book>();
}
