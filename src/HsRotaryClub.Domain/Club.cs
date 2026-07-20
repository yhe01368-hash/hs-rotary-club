using System.ComponentModel.DataAnnotations;

namespace HsRotaryClub.Domain;

/// <summary>
/// 扶輪社資料。每一個「社」(例:示範扶輪社、台中西北)是一個 Club entity。
/// 所有 Member / ClubCollection / FriendlyClub / MonthlyReceivableSpec 都掛 ClubId FK。
/// v0.7 開始支援多社共存於同一個 SQLite db。
/// </summary>
public class Club
{
    public int Id { get; set; }

    [Display(Name = "社名")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "分區")]
    public string? District { get; set; }

    [Display(Name = "授證日期")]
    public DateOnly? CharterDate { get; set; }

    [Display(Name = "聯絡人")]
    public string? Contact { get; set; }

    [Display(Name = "聯絡 Email")]
    public string? ContactEmail { get; set; }

    [Display(Name = "備註")]
    public string? Remarks { get; set; }

    [Display(Name = "啟用")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "建立時間")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
