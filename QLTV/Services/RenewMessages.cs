using LibraryManagement.Areas.Admin.Services;

namespace LibraryManagement.Services;

// Chuyển mã lý do không gia hạn được thành câu theo ngôn ngữ của người dùng
public static class RenewMessages
{
    public static string Localize(Loc t, RenewDecision d) => d.Code switch
    {
        "returned" => t["renew.err.returned"],
        "locked" => t["renew.err.locked"],
        "nocard" => t["renew.err.nocard"],
        "item_overdue" => t["renew.err.item_overdue"],
        "other_overdue" => t["renew.err.other_overdue"],
        "max_renewals" => t["renew.err.max_renewals", d.Max],
        _ => d.Error ?? string.Empty
    };
}
