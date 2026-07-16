namespace HsRotaryClub.Domain;

/// <summary>
/// 社員基本資料。
/// 對應舊版 <c>TS81.mdb</c> 主檔;欄位名、欄數大致依「社員基本資料維護」畫面擷取。
/// </summary>
public class Member
{
    public int Id { get; set; }

    /// <summary>社員編號 (舊版 3 位數, 121~139 等)</summary>
    public int Code { get; set; }

    /// <summary>社友姓名 (中文)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>英文姓名</summary>
    public string? EnglishName { get; set; }

    /// <summary>身份証字號</summary>
    public string? IdNumber { get; set; }

    public DateOnly? Birthday { get; set; }

    /// <summary>配偶姓名</summary>
    public string? SpouseName { get; set; }

    public DateOnly? WeddingAnniversary { get; set; }

    /// <summary>介紹人 (存社員編號)</summary>
    public int? ReferrerCode { get; set; }

    /// <summary>介紹人姓名 (非正規化,顯示用)</summary>
    public string? ReferrerName { get; set; }

    // 服務單位
    public string? EmployerName { get; set; }
    public string? EmployerTitle { get; set; }
    public string? EmployerAddress { get; set; }
    public string? EmployerZip { get; set; }
    public string? EmployerTel { get; set; }

    // 住宅處
    public string? HomeAddress { get; set; }
    public string? HomeZip { get; set; }

    /// <summary>組號 (4 位數字,舊版 MAT11_1 對照)</summary>
    public string? GroupNo { get; set; }

    /// <summary>入社日期 (舊版 入社日期 欄位)</summary>
    public DateOnly? JoinDate { get; set; }

    /// <summary>遷出日期 (離社)</summary>
    public DateOnly? LeaveDate { get; set; }

    public string? LeaveReason { get; set; }

    /// <summary>職業分類 (例如:不動產仲介)</summary>
    public string? Occupation { get; set; }

    public string? BusinessContent { get; set; }

    public string? Mobile { get; set; }

    public string? Email { get; set; }

    /// <summary>RID (Rotary International ID,例如:12213492)</summary>
    public string? Rid { get; set; }

    /// <summary>英文住址 (供國際信件用)</summary>
    public string? EnglishAddress { get; set; }

    /// <summary>已刪除 (軟刪旗標,舊版「顯示刪除社員」按鈕依此切)</summary>
    public bool IsDeleted { get; set; }

    /// <summary>速查索引 (Voter Index,舊版 139 社員 筆數 0063 等)</summary>
    public int SortOrder { get; set; }
}
