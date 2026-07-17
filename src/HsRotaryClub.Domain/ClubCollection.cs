using System.ComponentModel.DataAnnotations;

namespace HsRotaryClub.Domain;

/// <summary>
/// 例會收款記錄。
/// 對應舊版 <c>TS641.mdb</c>「例會收款記錄檔」主檔畫面:
/// 序 | 收款日期 | 收款類別 | 收現金 | 票據金額 | 合計金額 | 帳單 | 收款人。
/// </summary>
public class ClubCollection
{
    public int Id { get; set; }

    [Display(Name = "西元")]
    public int Year { get; set; }

    [Display(Name = "月份")]
    public int Month { get; set; }

    [Display(Name = "收款日期")]
    public DateOnly CollectionDate { get; set; }

    [Display(Name = "社員編號")]
    public int MemberCode { get; set; }

    [Display(Name = "社員姓名")]
    public string MemberName { get; set; } = string.Empty;

    [Display(Name = "收款類別")]
    public string Category { get; set; } = string.Empty;

    [Display(Name = "收現金")]
    public decimal CashAmount { get; set; }

    [Display(Name = "票據金額")]
    public decimal CheckAmount { get; set; }

    [Display(Name = "合計金額")]
    public decimal TotalAmount => CashAmount + CheckAmount;

    [Display(Name = "帳單")]
    public string? ReceiptNo { get; set; }

    [Display(Name = "收款人")]
    public string? Collector { get; set; }
}

/// <summary>
/// 每月例行應收主檔:定義每位社員每月應繳什麼項目。
/// 對應舊版「每月例行應收主檔」(TS641_m 或 MAT11_1 對照)。
/// </summary>
public class MonthlyReceivableSpec
{
    public int Id { get; set; }

    [Display(Name = "年度")]
    public int Year { get; set; }

    [Display(Name = "月份")]
    public int Month { get; set; }

    [Display(Name = "社員編號")]
    public int MemberCode { get; set; }

    [Display(Name = "社員姓名")]
    public string? MemberName { get; set; }

    [Display(Name = "應收項目")]
    public string Item { get; set; } = string.Empty;

    [Display(Name = "應收金額")]
    public decimal Amount { get; set; }

    [Display(Name = "已收金額")]
    public decimal SettledAmount { get; set; }

    [Display(Name = "未收金額")]
    public decimal OutstandingAmount => Amount - SettledAmount;
}
