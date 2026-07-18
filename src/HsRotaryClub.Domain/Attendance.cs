using System.ComponentModel.DataAnnotations;

namespace HsRotaryClub.Domain;

/// <summary>
/// v0.10 — 年度組別資料 (TS81.MAT1)。
/// 對應舊版畫面 「(2)年度組別資料對照檔」。
/// 一個社在特定年度會分若干組,每組有組長 + 組員。
/// </summary>
public class AttendanceGroup
{
    public int Id { get; set; }

    [Display(Name = "所屬社")]
    public int ClubId { get; set; } = ClubDefaults.DefaultClubId;

    [Display(Name = "年度")]
    public int Year { get; set; } = DateTime.Today.Year;

    /// <summary>組別 (A / B / C / 1 / 2 / 3 等)</summary>
    [Display(Name = "組別")]
    public string GroupName { get; set; } = "";

    /// <summary>組長社員編號</summary>
    [Display(Name = "組長")]
    public int GroupLeaderCode { get; set; }

    /// <summary>組長姓名 (denormalized)</summary>
    [Display(Name = "組長姓名")]
    public string GroupLeaderName { get; set; } = "";

    /// <summary>組員社員編號 (一個社可能多個組員;這裡只記一個)</summary>
    [Display(Name = "組員")]
    public int GroupMemberCode { get; set; }

    /// <summary>組員姓名 (denormalized)</summary>
    [Display(Name = "組員姓名")]
    public string GroupMemberName { get; set; } = "";

    [Display(Name = "應出席")]
    public int ShouldAttend { get; set; }

    [Display(Name = "實出席")]
    public int ActualAttend { get; set; }

    [Display(Name = "補出席")]
    public int MakeupAttend { get; set; }

    /// <summary>有效?</summary>
    public bool IsActive { get; set; } = true;

    [Display(Name = "備註")]
    public string? Remarks { get; set; }
}

/// <summary>
/// v0.10 — 出缺席記錄 (對應舊版 TS81.MAT11_1)。
/// </summary>
public enum AttendanceType
{
    Present = 1,    // 實出席
    Absent = 2,     // 缺席
    Makeup = 3,     // 補出席
    Excused = 4,    // 請假
}

public class AttendanceRecord
{
    public int Id { get; set; }

    [Display(Name = "所屬社")]
    public int ClubId { get; set; } = ClubDefaults.DefaultClubId;

    [Display(Name = "年度")]
    public int Year { get; set; }

    [Display(Name = "社員編號")]
    public int MemberCode { get; set; }

    [Display(Name = "社員姓名")]
    public string MemberName { get; set; } = "";

    /// <summary>例會日期</summary>
    [Display(Name = "例會日期")]
    public DateTime MeetingDate { get; set; }

    [Display(Name = "出席類型")]
    public AttendanceType Type { get; set; } = AttendanceType.Present;

    [Display(Name = "補出席日期")]
    public DateTime? MakeupDate { get; set; }

    [Display(Name = "備註")]
    public string? Remarks { get; set; }
}