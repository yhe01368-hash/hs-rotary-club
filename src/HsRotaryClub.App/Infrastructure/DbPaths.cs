using System.IO;
using Microsoft.EntityFrameworkCore;

namespace HsRotaryClub.App.Infrastructure;

/// <summary>
/// 集中管 SQLite db 實體路徑。
/// v0.1 寫到 <c>%LocalAppData%\HsRotaryClub\rotary.db</c>;
/// 之後改成 user-configurable 路徑 (multi-tenant 多社)。
/// </summary>
public static class DbPaths
{
    public static string Get()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HsRotaryClub");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "rotary.db");
    }
}
