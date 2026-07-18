using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HsRotaryClub.Infrastructure;

/// <summary>
/// v0.9.1 — Trial 限制 + LicenseAdmin 生成輔助。
///
/// Trial 規則 (沒 license.dat 時):
/// - 允許最多 1 個 active club
/// - 啟動後 7 天內有效,之後 Status 變 Expired (即使沒 license.dat)
///
/// LicenseAdmin 用法 (在開發者機器執行):
///   LicenseAdmin.IssueToMachine("豐原西南扶輪社", daysValid: 365, maxClubs: 5)
///   → 寫 license.dat + 印 MachineId
/// </summary>
public static class LicenseAdmin
{
    /// <summary>Trial 規則</summary>
    public const int TrialMaxClubs = 1;
    public static readonly TimeSpan TrialDuration = TimeSpan.FromDays(7);

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
    };

    /// <summary>檢查 trial 是否已過期 (無 license.dat 也算 trial)。summary></summary>
    public static bool IsTrialExpired()
    {
        var path = LicenseService.GetLicensePath();
        if (File.Exists(path))
        {
            // 有 license — 不是 trial
            return false;
        }
        // 找 trial marker (db 第一次建立時間 → AppData/HsRotaryClub/.trial-start)
        var marker = Path.Combine(Path.GetDirectoryName(path)!, ".trial-start");
        if (!File.Exists(marker))
        {
            File.WriteAllText(marker, DateTime.UtcNow.ToString("O"));
            return false;
        }
        var startStr = File.ReadAllText(marker).Trim();
        if (DateTime.TryParse(startStr, out var start))
        {
            return DateTime.UtcNow - start > TrialDuration;
        }
        return false;
    }

    /// <summary>取得 trial 開始時間,沒 marker 就建一個。</summary>
    public static DateTime GetTrialStart()
    {
        var marker = Path.Combine(
            Path.GetDirectoryName(LicenseService.GetLicensePath())!,
            ".trial-start");
        if (!File.Exists(marker))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(marker)!);
            File.WriteAllText(marker, DateTime.UtcNow.ToString("O"));
            return DateTime.UtcNow;
        }
        return DateTime.TryParse(File.ReadAllText(marker).Trim(), out var start)
            ? start
            : DateTime.UtcNow;
    }

    /// <summary>開發者用:為當前機器生成 license,寫 license.dat。回傳 MachineId 讓 user 抄。</summary>
    [SupportedOSPlatform("windows")]
    public static string IssueToMachine(string issuedTo, int daysValid = 365, int maxClubs = 5, bool bindToMachine = true)
    {
        var info = new LicenseInfo
        {
            IssuedTo = issuedTo,
            Issuer = "HsRotaryClub Admin (祐哥 / Chia Chang)",
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = daysValid > 0 ? DateTime.UtcNow.AddDays(daysValid) : null,
            MachineId = bindToMachine ? (LicenseService.GetMachineId() ?? "") : "",
            MaxClubs = maxClubs,
        };
        LicenseService.Issue(info);
        return info.MachineId;
    }

    /// <summary>驗 license 是否允許新增第 N 個 club。Trial 限制 1,Active 看 MaxClubs。</summary>
    public static (bool allowed, string reason) CanAddClub(LicenseInfo info, int currentActiveClubs)
    {
        if (info.Status == LicenseStatus.Expired || IsTrialExpired())
        {
            return (false, "License 已過期,無法新增社團");
        }
        if (info.Status == LicenseStatus.MachineMismatch)
        {
            return (false, "License 不屬於這台機器,無法新增社團");
        }
        if (info.Status == LicenseStatus.Corrupted || info.Status == LicenseStatus.NoMachineId)
        {
            return (false, "License 檔案問題,無法新增社團");
        }
        var max = info.Status == LicenseStatus.Trial ? TrialMaxClubs : info.MaxClubs;
        if (max > 0 && currentActiveClubs >= max)
        {
            return (false, $"已達 license 上限 ({max} 個社團)");
        }
        return (true, "OK");
    }
}