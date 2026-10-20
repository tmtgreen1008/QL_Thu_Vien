using LibraryManagement.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibraryManagement.Areas.Admin.ViewModels;

// Nhãn tiếng Việt cho các enum, dùng chung trong view
public static class EnumLabels
{
    public static string Status(CopyStatus s) => s switch
    {
        CopyStatus.Available => "Sẵn sàng",
        CopyStatus.OnLoan => "Đang mượn",
        CopyStatus.Lost => "Mất",
        CopyStatus.Damaged => "Hỏng",
        CopyStatus.Disposed => "Đã thanh lý",
        _ => s.ToString()
    };

    public static string StatusBadge(CopyStatus s) => s switch
    {
        CopyStatus.Available => "success",
        CopyStatus.OnLoan => "primary",
        CopyStatus.Lost => "danger",
        CopyStatus.Damaged => "warning",
        _ => "secondary"
    };

    public static string Condition(CopyCondition c) => c switch
    {
        CopyCondition.New => "Mới",
        CopyCondition.Good => "Tốt",
        CopyCondition.Worn => "Cũ",
        _ => c.ToString()
    };

    // Trạng thái Admin được phép đặt thủ công (Đang mượn do quy trình mượn trả đặt)
    public static List<SelectListItem> StatusOptions(CopyStatus? selected = null) =>
        new[] { CopyStatus.Available, CopyStatus.Lost, CopyStatus.Damaged, CopyStatus.Disposed }
            .Select(s => new SelectListItem(Status(s), ((int)s).ToString(), s == selected))
            .ToList();

    public static List<SelectListItem> ConditionOptions(CopyCondition? selected = null) =>
        new[] { CopyCondition.New, CopyCondition.Good, CopyCondition.Worn }
            .Select(c => new SelectListItem(Condition(c), ((int)c).ToString(), c == selected))
            .ToList();
}
