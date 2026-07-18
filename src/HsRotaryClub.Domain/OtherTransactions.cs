using System.ComponentModel.DataAnnotations;

namespace HsRotaryClub.Domain;

/// <summary>
/// v0.12 — M5 其它收入。
/// 對應舊版 「其它收支管理系統 / 其它收入作業 (TS14? or separate file)」。
/// </summary>
public class OtherIncome
{
    public int Id { get; set; }

    [Display(Name = "所屬社")]
    public int ClubId { get; set; } = ClubDefaults.DefaultClubId;

    [Display(Name = "產生日期")]
    public DateTime TxDate { get; set; } = DateTime.Today;

    [Display(Name = "西元")]
    public int Year { get; set; } = DateTime.Today.Year;

    [Display(Name = "月份")]
    public int Month { get; set; } = DateTime.Today.Month;

    [Display(Name = "單據號碼")]
    public string? VoucherNo { get; set; }

    [Display(Name = "序")]
    public int Seq { get; set; }

    /// <summary>對象 (例:某友社贊助 / 銀行利息)</summary>
    [Display(Name = "對象")]
    public string? Subject { get; set; }

    [Display(Name = "說明")]
    public string? Description { get; set; }

    [Display(Name = "金額")]
    public decimal Amount { get; set; }

    [Display(Name = "收入類別")]
    public string? Category { get; set; }
}

/// <summary>
/// v0.12 — M5 扶輪月支出 (對應舊版 TS14)。
/// </summary>
public class MonthlyExpense
{
    public int Id { get; set; }

    [Display(Name = "所屬社")]
    public int ClubId { get; set; } = ClubDefaults.DefaultClubId;

    [Display(Name = "產生日期")]
    public DateTime TxDate { get; set; } = DateTime.Today;

    [Display(Name = "西元")]
    public int Year { get; set; } = DateTime.Today.Year;

    [Display(Name = "月份")]
    public int Month { get; set; } = DateTime.Today.Month;

    [Display(Name = "單據號碼")]
    public string? VoucherNo { get; set; }

    [Display(Name = "對象")]
    public string? Subject { get; set; }

    [Display(Name = "說明")]
    public string? Description { get; set; }

    /// <summary>貸方科目代號</summary>
    [Display(Name = "貸方科目")]
    public string? CreditAccount { get; set; }

    [Display(Name = "金額")]
    public decimal Amount { get; set; }

    [Display(Name = "支出類別")]
    public string? Category { get; set; }
}