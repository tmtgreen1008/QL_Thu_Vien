using System.Reflection;
using Microsoft.Extensions.Localization;

namespace LibraryManagement.Services;

// Lấy chuỗi giao diện theo ngôn ngữ hiện tại (vi mặc định, en) từ Resources/SharedResource*.resx.
// Dùng trong view: @T["nav.home"]  hoặc  @T["book.available", 3]
public class Loc
{
    private readonly IStringLocalizer _localizer;

    public Loc(IStringLocalizerFactory factory)
    {
        _localizer = factory.Create("SharedResource", Assembly.GetExecutingAssembly().GetName().Name!);
    }

    public string this[string key] => _localizer[key].Value;

    public string this[string key, params object[] args] => _localizer[key, args].Value;
}
