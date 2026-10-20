namespace LibraryManagement.Areas.Admin.ViewModels;

// Một dòng trong danh sách Tác giả / Thể loại
public class NamedItemRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Extra { get; set; }      // Tiểu sử hoặc mô tả
    public int BookCount { get; set; }
}
