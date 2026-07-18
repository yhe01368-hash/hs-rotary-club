using System.Text.Json.Serialization;

namespace HsRotaryClub.Infrastructure;

/// <summary>
/// v0.9 — license 狀態。
/// Loaded from %LocalAppData%/HsRotaryClub/license.dat on App.OnStartup。
/// </summary>
public enum LicenseStatus
{
    /// <summary>沒 license.dat — 跑 trial mode (最多 1 個 club / 7 天)</summary>
    Trial,
    /// <summary>license 有效</summary>
    Active,
    /// <summary>license 已過期</summary>
    Expired,
    /// <summary>license 是別台機器的 (machine mismatch)</summary>
    MachineMismatch,
    /// <summary>license 檔案壞掉 / 簽章不對</summary>
    Corrupted,
    /// <summary>無法取得 machine id (機器無 disk serial)</summary>
    NoMachineId,
}

public sealed class LicenseInfo
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>發行給誰 — 例如「豐原西南扶輪社」</summary>
    [JsonPropertyName("issuedTo")]
    public string IssuedTo { get; set; } = "";

    /// <summary>發行者 — 例如「HsRotaryClub Admin (祐哥)」</summary>
    [JsonPropertyName("issuer")]
    public string Issuer { get; set; } = "";

    /// <summary>發行日期</summary>
    [JsonPropertyName("issuedAt")]
    public DateTime IssuedAt { get; set; }

    /// <summary>到期日 — null = 永久</summary>
    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>允許的 MachineId (空字串 = 不綁機)</summary>
    [JsonPropertyName("machineId")]
    public string MachineId { get; set; } = "";

    /// <summary>最大社團數量 — 0 = 不限</summary>
    [JsonPropertyName("maxClubs")]
    public int MaxClubs { get; set; } = 0;

    /// <summary>有效?</summary>
    public LicenseStatus Status { get; set; } = LicenseStatus.Trial;
}