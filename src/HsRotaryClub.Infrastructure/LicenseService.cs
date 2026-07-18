using System.IO;
using System.Management;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HsRotaryClub.Infrastructure;

/// <summary>
/// v0.9 — license 載入/驗證/發行。
///
/// 設計:
/// - license.dat 放 %LocalAppData%/HsRotaryClub/license.dat
/// - 內容: JSON + HMAC-SHA256 簽章
/// - 簽章 key 編在 binary 內(簡單防拷;高安全要靠 SmartCard 等)
/// - MachineId: WMI Win32_DiskDrive → SerialNumber (or fallback to Environment.MachineName + UserName)
/// - Trial mode: 沒 license.dat → Status=Trial,Allow 1 club 7 天
/// </summary>
public static class LicenseService
{
    private const string LicenseFileName = "license.dat";
    private const string LicenseDir = "";  // %LocalAppData%/HsRotaryClub
    private const string SecretKey = "HsRotaryClub-v0.9-LICENSE-KEY-2026";  // production 應該用更安全的方式

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
    };

    /// <summary>取得 license.dat 完整路徑。</summary>
    public static string GetLicensePath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HsRotaryClub");
        return Path.Combine(dir, LicenseFileName);
    }

    /// <summary>取得本機 machine id。失敗時回傳 null。</summary>
    [SupportedOSPlatform("windows")]
    public static string? GetMachineId()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive");
            foreach (var obj in searcher.Get())
            {
                var sn = obj["SerialNumber"]?.ToString()?.Trim();
                if (!string.IsNullOrWhiteSpace(sn) && sn != "0")
                {
                    return sn;
                }
            }
        }
        catch
        {
            // WMI 不支援 / 沒權限
        }
        // fallback: machine + user
        var fallback = $"{Environment.MachineName}\\{Environment.UserName}";
        return string.IsNullOrWhiteSpace(fallback) ? null : fallback;
    }

    /// <summary>載入 + 驗證 license。回傳 LicenseInfo 帶 Status。</summary>
    [SupportedOSPlatform("windows")]
    public static LicenseInfo LoadAndValidate()
    {
        var path = GetLicensePath();
        if (!File.Exists(path))
        {
            return new LicenseInfo
            {
                Status = LicenseStatus.Trial,
                IssuedAt = DateTime.UtcNow,
            };
        }

        try
        {
            var content = File.ReadAllText(path);
            // 統一換行 (Windows \r\n vs Linux \n)
            content = content.Replace("\r\n", "\n").Replace("\r", "\n");
            // 格式: 第一行 JSON, 第二行 "---", 第三行 hex signature
            var lines = content.Split('\n');
            if (lines.Length < 3 || lines[1].Trim() != "---")
            {
                return new LicenseInfo { Status = LicenseStatus.Corrupted };
            }

            var json = lines[0].TrimEnd('\n').Trim();
            var sigHex = lines[2].Trim();

            // 驗簽
            var keyBytes = Encoding.UTF8.GetBytes(SecretKey);
            using var hmac = new HMACSHA256(keyBytes);
            var dataBytes = Encoding.UTF8.GetBytes(json);
            var sigBytes = hmac.ComputeHash(dataBytes);
            var sigHexActual = Convert.ToHexString(sigBytes);

            if (!string.Equals(sigHexActual, sigHex, StringComparison.OrdinalIgnoreCase))
            {
                return new LicenseInfo { Status = LicenseStatus.Corrupted };
            }

            var info = JsonSerializer.Deserialize<LicenseInfo>(json, JsonOpts);
            if (info is null)
            {
                return new LicenseInfo { Status = LicenseStatus.Corrupted };
            }

            // 驗 machine
            if (!string.IsNullOrEmpty(info.MachineId))
            {
                var mid = GetMachineId();
                if (mid is null)
                {
                    info.Status = LicenseStatus.NoMachineId;
                    return info;
                }
                if (!string.Equals(mid, info.MachineId, StringComparison.OrdinalIgnoreCase))
                {
                    info.Status = LicenseStatus.MachineMismatch;
                    return info;
                }
            }

            // 驗過期
            if (info.ExpiresAt.HasValue && info.ExpiresAt.Value < DateTime.UtcNow)
            {
                info.Status = LicenseStatus.Expired;
                return info;
            }

            info.Status = LicenseStatus.Active;
            return info;
        }
        catch
        {
            return new LicenseInfo { Status = LicenseStatus.Corrupted };
        }
    }

    /// <summary>發行 (生成) 一個新的 license,寫到 license.dat。回傳寫入後的內容。</summary>
    public static string Issue(LicenseInfo info)
    {
        info.IssuedAt = info.IssuedAt == default ? DateTime.UtcNow : info.IssuedAt;
        var json = JsonSerializer.Serialize(info, JsonOpts);
        var keyBytes = Encoding.UTF8.GetBytes(SecretKey);
        using var hmac = new HMACSHA256(keyBytes);
        var sig = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(json)));

        var path = GetLicensePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var content = $"{json}\n---\n{sig}";
        File.WriteAllText(path, content);
        return content;
    }

    /// <summary>回傳 license 對 user 的可讀說明 (放在狀態列)。</summary>
    public static string Describe(LicenseInfo info)
    {
        return info.Status switch
        {
            LicenseStatus.Trial => "🟡 試用模式 (Trial) — 1 社 / 7 天",
            LicenseStatus.Active => $"🟢 已授權: {info.IssuedTo}{(info.ExpiresAt.HasValue ? $" 到期 {info.ExpiresAt.Value:yyyy-MM-dd}" : " 永久")}",
            LicenseStatus.Expired => $"🔴 已過期 (到期 {info.ExpiresAt:yyyy-MM-dd})",
            LicenseStatus.MachineMismatch => "🔴 License 不屬於這台機器",
            LicenseStatus.Corrupted => "🔴 License 檔損壞",
            LicenseStatus.NoMachineId => "🔴 抓不到本機 ID,無法驗證",
            _ => "❓ 未知",
        };
    }
}