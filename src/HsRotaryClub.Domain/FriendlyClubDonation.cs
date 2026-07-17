using System.ComponentModel.DataAnnotations;

namespace HsRotaryClub.Domain;

/// <summary>
/// 友社 (兄弟社) 對照檔。
/// 對應舊版「社團名稱對照檔」(T552)。
/// </summary>
public class FriendlyClub
{
    public int Id { get; set; }

    [Display(Name = "社團代號")]
    public string ClubCode { get; set; } = string.Empty;

    [Display(Name = "社團名稱")]
    public string ClubName { get; set; } = string.Empty;

    [Display(Name = "備註")]
    public string? Remarks { get; set; }

    [Display(Name = "啟用")]
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// 友社捐款收支記錄。
/// 對應舊版「友社社團捐款收入作業」畫面:例會捐款給兄弟社、或兄弟社捐款給本會。
/// </summary>
public class ClubDonation
{
    public int Id { get; set; }

    [Display(Name = "日期")]
    public DateOnly TxDate { get; set; }

    [Display(Name = "友社")]
    public int FriendlyClubId { get; set; }

    [Display(Name = "友社名稱")]
    public string FriendlyClubName { get; set; } = string.Empty;  // 反正規顯示用

    [Display(Name = "方向")]
    public DonationDirection Direction { get; set; }

    [Display(Name = "金額")]
    public decimal Amount { get; set; }

    [Display(Name = "用途")]
    public string? Purpose { get; set; }

    [Display(Name = "收據編號")]
    public string? ReceiptNo { get; set; }
}

public enum DonationDirection
{
    /// <summary>本社支付給友社</summary>
    Out = 0,
    /// <summary>友社支付給本社</summary>
    In = 1,
}
