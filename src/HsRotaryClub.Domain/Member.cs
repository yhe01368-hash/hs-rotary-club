using System.ComponentModel.DataAnnotations;

namespace HsRotaryClub.Domain;

/// <summary>
/// v0.7 — 各 entity 歸屬某個 Club (ClubId FK)。預設值 = 1 (預設社「示範扶輪社」)
/// 由 SeedData 在 db migrate 後寫入 id=1。
/// </summary>
public static class ClubDefaults
{
    public const int DefaultClubId = 1;
}

/// <summary>
/// 社員基本資料。
/// 對應舊版 <c>TS81.mdb</c> 主檔;欄位名大致依「社員基本資料維護」畫面擷取。
/// v0.7 開始歸屬某個 Club (ClubId FK)。
/// </summary>
public class Member
{
    public int Id { get; set; }

    /// <summary>FK Club — v0.7 A2 引入</summary>
    [Display(Name = "所屬社")]
    public int ClubId { get; set; } = ClubDefaults.DefaultClubId;

    [Display(Name = "社員編號")]
    public int Code { get; set; }

    [Display(Name = "社友姓名")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "英文名字")]
    public string? EnglishName { get; set; }

    [Display(Name = "身份証字號")]
    public string? IdNumber { get; set; }

    [Display(Name = "生日")]
    public DateOnly? Birthday { get; set; }

    [Display(Name = "配偶姓名")]
    public string? SpouseName { get; set; }

    [Display(Name = "結婚紀念日")]
    public DateOnly? WeddingAnniversary { get; set; }

    /// <summary>介紹人 (存社員編號)</summary>
    [Display(Name = "介紹人編號")]
    public int? ReferrerCode { get; set; }

    /// <summary>介紹人姓名 (非正規化,顯示用)</summary>
    [Display(Name = "介紹人")]
    public string? ReferrerName { get; set; }

    [Display(Name = "服務單位名稱")]
    public string? EmployerName { get; set; }

    [Display(Name = "職稱")]
    public string? EmployerTitle { get; set; }

    [Display(Name = "服務單位地址")]
    public string? EmployerAddress { get; set; }

    [Display(Name = "服務單位區號")]
    public string? EmployerZip { get; set; }

    [Display(Name = "服務單位電話")]
    public string? EmployerTel { get; set; }

    [Display(Name = "住宅處地址")]
    public string? HomeAddress { get; set; }

    [Display(Name = "住宅處區號")]
    public string? HomeZip { get; set; }

    [Display(Name = "組號")]
    public string? GroupNo { get; set; }

    [Display(Name = "入社日期")]
    public DateOnly? JoinDate { get; set; }

    [Display(Name = "遷出日期")]
    public DateOnly? LeaveDate { get; set; }

    [Display(Name = "遷出原因")]
    public string? LeaveReason { get; set; }

    [Display(Name = "職業分類")]
    public string? Occupation { get; set; }

    [Display(Name = "業務內容")]
    public string? BusinessContent { get; set; }

    [Display(Name = "手機")]
    public string? Mobile { get; set; }

    [Display(Name = "電子信箱")]
    public string? Email { get; set; }

    [Display(Name = "RID")]
    public string? Rid { get; set; }

    [Display(Name = "英文住址")]
    public string? EnglishAddress { get; set; }

    /// <summary>
    /// 現任旗標 (舊版「現任社員 / 顯示刪除社員」toggle 依此切)。
    /// true = 現任在籍;false = 已遷出 / 刪除 (archive only)。
    /// </summary>
    [Display(Name = "現任")]
    public bool IsCurrent { get; set; } = true;

    /// <summary>速查索引 (舊版 139 社員 筆數 0063 等)</summary>
    [Display(Name = "序號")]
    public int SortOrder { get; set; }
}
