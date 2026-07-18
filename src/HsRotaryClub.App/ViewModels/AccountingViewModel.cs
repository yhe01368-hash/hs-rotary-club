using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HsRotaryClub.App.Infrastructure;
using HsRotaryClub.Domain;
using HsRotaryClub.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HsRotaryClub.App.ViewModels;

/// <summary>
/// v0.13 — M6 會計月報表 (對應舊版 「(1)豐原西南扶輪社 XXXX年度 - XXXX年X月份會計月報表」)。
/// 左: 收入科目 (本月收入/累計收入/預算/執行率)
/// 右: 支出科目 (本月支出/累計支出/預算/執行率)
/// </summary>
public partial class AccountingViewModel : ObservableObject
{
    private readonly RotaryDbContext _db;
    private readonly CurrentClubContext _currentClub;

    public ObservableCollection<MonthlyReportRow> IncomeRows { get; } = new();
    public ObservableCollection<MonthlyReportRow> ExpenseRows { get; } = new();

    [ObservableProperty]
    private int _year = DateTime.Today.Year;

    [ObservableProperty]
    private int _month = DateTime.Today.Month;

    [ObservableProperty]
    private string _statusMessage = "就緒";

    public decimal IncomeThisMonthTotal => IncomeRows.Sum(r => r.ThisMonth);
    public decimal IncomeYtdTotal => IncomeRows.Sum(r => r.Ytd);
    public decimal IncomeBudgetTotal => IncomeRows.Sum(r => r.AnnualBudget);
    public decimal ExpenseThisMonthTotal => ExpenseRows.Sum(r => r.ThisMonth);
    public decimal ExpenseYtdTotal => ExpenseRows.Sum(r => r.Ytd);
    public decimal ExpenseBudgetTotal => ExpenseRows.Sum(r => r.AnnualBudget);

    public AccountingViewModel(RotaryDbContext db, CurrentClubContext currentClub)
    {
        _db = db;
        _currentClub = currentClub;
        Reload();
        _currentClub.CurrentClubIdChanged += (_, _) => Reload();
    }

    partial void OnYearChanged(int value) => Reload();
    partial void OnMonthChanged(int value) => Reload();

    [RelayCommand]
    private void Reload()
    {
        IncomeRows.Clear();
        ExpenseRows.Clear();

        var subjects = _db.AccountSubjects.AsNoTracking()
            .Where(s => s.ClubId == _currentClub.CurrentClubId).ToList();
        var entries = _db.AccountEntries.AsNoTracking()
            .Where(e => e.ClubId == _currentClub.CurrentClubId && e.Year == Year && e.Month <= Month)
            .ToList();

        foreach (var s in subjects.Where(s => s.Type == AccountType.Income).OrderBy(s => s.Order))
        {
            IncomeRows.Add(new MonthlyReportRow
            {
                Code = s.Code,
                Name = s.Name,
                ThisMonth = entries.Where(e => e.SubjectCode == s.Code && e.Month == Month).Sum(e => e.ThisMonth),
                Ytd = entries.Where(e => e.SubjectCode == s.Code).Sum(e => e.ThisMonth),
                AnnualBudget = s.AnnualBudget,
            });
        }
        foreach (var s in subjects.Where(s => s.Type == AccountType.Expense).OrderBy(s => s.Order))
        {
            ExpenseRows.Add(new MonthlyReportRow
            {
                Code = s.Code,
                Name = s.Name,
                ThisMonth = entries.Where(e => e.SubjectCode == s.Code && e.Month == Month).Sum(e => e.ThisMonth),
                Ytd = entries.Where(e => e.SubjectCode == s.Code).Sum(e => e.ThisMonth),
                AnnualBudget = s.AnnualBudget,
            });
        }

        OnPropertyChanged(nameof(IncomeThisMonthTotal));
        OnPropertyChanged(nameof(IncomeYtdTotal));
        OnPropertyChanged(nameof(IncomeBudgetTotal));
        OnPropertyChanged(nameof(ExpenseThisMonthTotal));
        OnPropertyChanged(nameof(ExpenseYtdTotal));
        OnPropertyChanged(nameof(ExpenseBudgetTotal));
        StatusMessage = $"載入 {IncomeRows.Count} 收入科目 / {ExpenseRows.Count} 支出科目 ({Year}-{Month:D2})";
    }

    [RelayCommand]
    private void AddSubject()
    {
        var s = new AccountSubject
        {
            ClubId = _currentClub.CurrentClubId,
            Type = AccountType.Income,
            Code = $"NEW{IncomeRows.Count + ExpenseRows.Count + 1:D2}",
            Name = "新科目",
            AnnualBudget = 0m,
        };
        _db.AccountSubjects.Add(s);
        if (!_db.TrySaveChanges(out var err))
        {
            StatusMessage = $"新增失敗: {err}";
            return;
        }
        Reload();
    }
}

public class MonthlyReportRow
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal ThisMonth { get; set; }
    public decimal Ytd { get; set; }
    public decimal AnnualBudget { get; set; }
    public double ExecutionRate => AnnualBudget == 0 ? 0 : (double)(Ytd / AnnualBudget);
    public string ExecutionRateText => $"{ExecutionRate:P1}";
}