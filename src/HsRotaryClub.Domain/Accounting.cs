using System.ComponentModel.DataAnnotations;

namespace HsRotaryClub.Domain;

/// <summary>
/// v0.12 — M6 會計月報表科目 (對應舊版 「會計月報表」)。
/// 收入/支出 各自的科目 + 預算 + 累計。
/// </summary>
public class AccountSubject
{
    public int Id { get; set; }

    [Display(Name = "所屬社")]
    public int ClubId { get; set; } = ClubDefaults.DefaultClubId;

    /// <summary>Income 收入 / Expense 支出</summary>
    [Display(Name = "類型")]
    public AccountType Type { get; set; } = AccountType.Income;

    [Display(Name = "科目代號")]
    public string Code { get; set; } = "";

    [Display(Name = "科目名稱")]
    public string Name { get; set; } = "";

    [Display(Name = "年度預算")]
    public decimal AnnualBudget { get; set; }

    /// <summary>順序</summary>
    [Display(Name = "排序")]
    public int Order { get; set; }
}

public enum AccountType
{
    Income = 1,   // 收入
    Expense = 2,  // 支出
}

/// <summary>
/// v0.12 — 月度會計事實 (收支實際發生金額)。
/// 每筆對應到 AccountSubject 的累積。
/// </summary>
public class AccountEntry
{
    public int Id { get; set; }

    [Display(Name = "所屬社")]
    public int ClubId { get; set; } = ClubDefaults.DefaultClubId;

    [Display(Name = "年度")]
    public int Year { get; set; }

    [Display(Name = "月份")]
    public int Month { get; set; }

    [Display(Name = "科目代號")]
    public string SubjectCode { get; set; } = "";

    [Display(Name = "本月金額")]
    public decimal ThisMonth { get; set; }
}