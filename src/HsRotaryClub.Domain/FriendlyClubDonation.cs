namespace HsRotaryClub.Domain;

/// <summary>
/// 友社 (兄弟社) 對照檔。
/// 對應舊版「社團名稱對照檔」(T552)。
/// </summary>
public class FriendlyClub
{
    public int Id { get; set; }

    /// <summary>社團代號</summary>
    public string ClubCode { get; set; } = string.Empty;

    /// <summary>社團名稱</summary>
    public string ClubName { get; set; } = string.Empty;

    public string? Remarks { get; set; }

    /// <summary>對外捐款 / 收受捐款往來是否啟用</summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// 友社捐款收支記錄。
/// 對應舊版「友社社團捐款收入作業」畫面:例會捐款給兄弟社、或兄弟社捐款給本會。
/// </summary>
public class ClubDonation
{
    public int Id { get; set; }

    public DateOnly TxDate { get; set; }

    /// <summary>FK to FriendlyClub</summary>
    public int FriendlyClubId { get; set; }

    /// <summary>捐款方向:Out = 本社付給友社 / In = 友社付給本社</summary>
    public DonationDirection Direction { get; set; }

    public decimal Amount { get; set; }

    /// <summary>用途 / 事由 (例:例會紀念、聯合例會、扶輪基金)</summary>
    public string? Purpose { get; set; }

    public string? ReceiptNo { get; set; }
}

public enum DonationDirection
{
    /// <summary>本社支付給友社</summary>
    Out = 0,
    /// <summary>友社支付給本社</summary>
    In = 1,
}
