namespace HsRotaryClub.Domain;

/// <summary>
/// 例會收款記錄。
/// 對應舊版 <c>TS641.mdb</c>「例會收款記錄檔」主檔畫面:
/// 序 | 收款日期 | 收款類別 | 收現金 | 票據金額 | 合計金額 | 帳單 | 收款人。
/// </summary>
public class ClubCollection
{
    public int Id { get; set; }

    /// <summary>西元年度 (例:2026)</summary>
    public int Year { get; set; }

    /// <summary>月份 (1~12)</summary>
    public int Month { get; set; }

    /// <summary>收款日期</summary>
    public DateOnly CollectionDate { get; set; }

    /// <summary>收款的社員 (FK to Member)</summary>
    public int MemberCode { get; set; }

    /// <summary>收款類別 (會費 / 臨時捐款 / 例餐 / 雜項 ...)</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>收現金</summary>
    public decimal CashAmount { get; set; }

    /// <summary>票據金額</summary>
    public decimal CheckAmount { get; set; }

    /// <summary>合計金額 = CashAmount + CheckAmount</summary>
    public decimal TotalAmount => CashAmount + CheckAmount;

    /// <summary>帳單 / 收據編號</summary>
    public string? ReceiptNo { get; set; }

    /// <summary>收款人 (記錄誰經手)</summary>
    public string? Collector { get; set; }
}

/// <summary>
/// 每月例行應收主檔:定義每位社員每月應繳什麼項目。
/// 對應舊版「每月例行應收主檔」(TS641_m 或 MAT11_1 對照)。
/// </summary>
public class MonthlyReceivableSpec
{
    public int Id { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }

    /// <summary>應收對象 (FK to Member)</summary>
    public int MemberCode { get; set; }

    /// <summary>應收項目 (例:例餐費 / 會費 / 基金會捐款 / RI捐款 ...)</summary>
    public string Item { get; set; } = string.Empty;

    /// <summary>應收金額</summary>
    public decimal Amount { get; set; }

    /// <summary>已實際收入金額 (跨多筆收款累計)</summary>
    public decimal SettledAmount { get; set; }

    public decimal OutstandingAmount => Amount - SettledAmount;
}
