using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HsRotaryClub.App.Infrastructure;
using HsRotaryClub.Domain;
using HsRotaryClub.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HsRotaryClub.App.ViewModels;

/// <summary>
/// v0.13 — M5 其它收支 (左邊收入 / 右邊支出 / 上方年度+月份 filter)。
/// 對應舊版 「(1)扶輪月支出作業」+ 「其它收入作業」。
/// </summary>
public partial class OtherTransactionViewModel : ObservableObject
{
    private readonly RotaryDbContext _db;
    private readonly CurrentClubContext _currentClub;

    public ObservableCollection<OtherIncome> Incomes { get; } = new();
    public ObservableCollection<MonthlyExpense> Expenses { get; } = new();

    [ObservableProperty]
    private int _year = DateTime.Today.Year;

    [ObservableProperty]
    private int _month = DateTime.Today.Month;

    [ObservableProperty]
    private OtherIncome? _selectedIncome;

    [ObservableProperty]
    private MonthlyExpense? _selectedExpense;

    [ObservableProperty]
    private string _statusMessage = "就緒";

    public OtherTransactionViewModel(RotaryDbContext db, CurrentClubContext currentClub)
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
        Incomes.Clear();
        var q = _db.OtherIncomes.AsNoTracking()
            .Where(o => o.ClubId == _currentClub.CurrentClubId && o.Year == Year && o.Month == Month);
        foreach (var o in q.OrderByDescending(o => o.TxDate).ToList())
            Incomes.Add(o);

        Expenses.Clear();
        var qe = _db.MonthlyExpenses.AsNoTracking()
            .Where(m => m.ClubId == _currentClub.CurrentClubId && m.Year == Year && m.Month == Month);
        foreach (var e in qe.OrderByDescending(e => e.TxDate).ToList())
            Expenses.Add(e);

        SelectedIncome ??= Incomes.FirstOrDefault();
        SelectedExpense ??= Expenses.FirstOrDefault();
        StatusMessage = $"載入 {Incomes.Count} 筆收入 / {Expenses.Count} 筆支出 ({Year}-{Month:D2})";
    }

    [RelayCommand]
    private void AddIncome()
    {
        var o = new OtherIncome
        {
            ClubId = _currentClub.CurrentClubId,
            Year = Year, Month = Month,
            TxDate = new DateTime(Year, Month, 1),
            Amount = 0m,
            Subject = "新收入",
            Category = "一般",
        };
        _db.OtherIncomes.Add(o);
        if (!_db.TrySaveChanges(out var err))
        {
            StatusMessage = $"新增失敗: {err}";
            return;
        }
        Reload();
        SelectedIncome = Incomes.FirstOrDefault(x => x.Id == o.Id);
    }

    [RelayCommand]
    private void SaveIncome()
    {
        if (SelectedIncome is null) return;
        var attached = _db.OtherIncomes.FirstOrDefault(o => o.Id == SelectedIncome.Id);
        if (attached is null) return;
        _db.Entry(attached).CurrentValues.SetValues(SelectedIncome);
        if (!_db.TrySaveChanges(out var err))
        {
            StatusMessage = $"儲存失敗: {err}";
            return;
        }
        StatusMessage = $"已儲存收入 {attached.Subject}";
        Reload();
    }

    [RelayCommand]
    private void DeleteIncome()
    {
        if (SelectedIncome is null) return;
        _db.OtherIncomes.Remove(SelectedIncome);
        if (!_db.TrySaveChanges(out var err))
        {
            StatusMessage = $"刪除失敗: {err}";
            return;
        }
        Reload();
    }

    [RelayCommand]
    private void AddExpense()
    {
        var e = new MonthlyExpense
        {
            ClubId = _currentClub.CurrentClubId,
            Year = Year, Month = Month,
            TxDate = new DateTime(Year, Month, 1),
            Amount = 0m,
            Subject = "新支出",
            Category = "一般",
        };
        _db.MonthlyExpenses.Add(e);
        if (!_db.TrySaveChanges(out var err))
        {
            StatusMessage = $"新增失敗: {err}";
            return;
        }
        Reload();
        SelectedExpense = Expenses.FirstOrDefault(x => x.Id == e.Id);
    }

    [RelayCommand]
    private void SaveExpense()
    {
        if (SelectedExpense is null) return;
        var attached = _db.MonthlyExpenses.FirstOrDefault(e => e.Id == SelectedExpense.Id);
        if (attached is null) return;
        _db.Entry(attached).CurrentValues.SetValues(SelectedExpense);
        if (!_db.TrySaveChanges(out var err))
        {
            StatusMessage = $"儲存失敗: {err}";
            return;
        }
        StatusMessage = $"已儲存支出 {attached.Subject}";
        Reload();
    }

    [RelayCommand]
    private void DeleteExpense()
    {
        if (SelectedExpense is null) return;
        _db.MonthlyExpenses.Remove(SelectedExpense);
        if (!_db.TrySaveChanges(out var err))
        {
            StatusMessage = $"刪除失敗: {err}";
            return;
        }
        Reload();
    }

    public decimal TotalIncome => Incomes.Sum(o => o.Amount);
    public decimal TotalExpense => Expenses.Sum(e => e.Amount);
    public decimal NetAmount => TotalIncome - TotalExpense;
}