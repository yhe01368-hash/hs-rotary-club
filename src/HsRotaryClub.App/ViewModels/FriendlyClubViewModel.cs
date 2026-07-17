using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HsRotaryClub.App.Controls;
using HsRotaryClub.Domain;
using HsRotaryClub.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HsRotaryClub.App.ViewModels;

/// <summary>
/// M3 友社捐款完整版。
/// 左速查(社團) + 右表單(社團基本資料) + 下方捐款記錄子表。
/// </summary>
public partial class FriendlyClubViewModel : ObservableObject
{
    private readonly RotaryDbContext _db;

    public ObservableCollection<FriendlyClub> Clubs { get; } = new();
    public ObservableCollection<ClubDonation> Donations { get; } = new();

    [ObservableProperty]
    private FriendlyClub? _selected;

    [ObservableProperty]
    private ClubDonation? _selectedDonation;

    [ObservableProperty]
    private string _filter = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "就緒";

    [ObservableProperty]
    private DateOnly _newDonationDate = DateOnly.FromDateTime(DateTime.Today);

    [ObservableProperty]
    private decimal _newDonationAmount;

    [ObservableProperty]
    private DonationDirection _newDonationDirection = DonationDirection.Out;

    [ObservableProperty]
    private string _newDonationPurpose = "例會紀念";

    public decimal InTotal => Donations.Where(d => d.Direction == DonationDirection.In).Sum(d => d.Amount);
    public decimal OutTotal => Donations.Where(d => d.Direction == DonationDirection.Out).Sum(d => d.Amount);
    public decimal NetTotal => InTotal - OutTotal;

    public FriendlyClubViewModel(RotaryDbContext db)
    {
        _db = db;
        Reload();
    }

    partial void OnFilterChanged(string value) => Reload();
    partial void OnSelectedChanged(FriendlyClub? value) => ReloadDonations();

    [RelayCommand]
    private void Reload()
    {
        var q = _db.FriendlyClubs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(Filter))
        {
            q = q.Where(c => c.ClubCode.Contains(Filter) || c.ClubName.Contains(Filter));
        }
        Clubs.Clear();
        foreach (var c in q.OrderBy(c => c.ClubCode).ToList())
            Clubs.Add(c);
        Selected ??= Clubs.FirstOrDefault();
        ReloadDonations();
        StatusMessage = $"載入 {Clubs.Count} 個友社";
    }

    [RelayCommand]
    private void ReloadDonations()
    {
        Donations.Clear();
        if (Selected is null) return;
        var rows = _db.ClubDonations
            .AsNoTracking()
            .Where(d => d.FriendlyClubId == Selected.Id)
            .OrderByDescending(d => d.TxDate)
            .ToList();
        foreach (var d in rows) Donations.Add(d);
        OnPropertyChanged(nameof(InTotal));
        OnPropertyChanged(nameof(OutTotal));
        OnPropertyChanged(nameof(NetTotal));
    }

    [RelayCommand]
    private void AddClub()
    {
        var next = "NEW01";
        if (_db.FriendlyClubs.Any())
        {
            var maxNum = _db.FriendlyClubs
                .Where(c => c.ClubCode.StartsWith("NEW"))
                .Select(c => c.ClubCode)
                .ToList();
            for (int i = 1; i < 1000; i++)
            {
                var candidate = $"NEW{i:D2}";
                if (!maxNum.Contains(candidate))
                {
                    next = candidate;
                    break;
                }
            }
        }
        var c = new FriendlyClub { ClubCode = next, ClubName = "新友社", IsActive = true };
        _db.FriendlyClubs.Add(c);
        if (!_db.TrySaveChanges(out var error))
        {
            StatusMessage = $"新增失敗: {error}";
            return;
        }
        Reload();
        Selected = Clubs.FirstOrDefault(x => x.Id == c.Id);
    }

    [RelayCommand]
    private void SaveClub()
    {
        if (Selected is null) return;
        var attached = _db.FriendlyClubs.FirstOrDefault(c => c.Id == Selected.Id);
        if (attached is null) { StatusMessage = "DB 找不到"; return; }
        _db.Entry(attached).CurrentValues.SetValues(Selected);
        if (!_db.TrySaveChanges(out var error))
        {
            StatusMessage = $"儲存失敗: {error}";
            return;
        }
        StatusMessage = $"已儲存 {attached.ClubName}";
    }

    [RelayCommand]
    private void DeleteClub()
    {
        if (Selected is null) return;
        var attached = _db.FriendlyClubs.FirstOrDefault(c => c.Id == Selected.Id);
        if (attached is null) return;

        // 反查有無捐款 FK — DeleteBehavior.Restrict 阻擋,但先給好訊息
        var hasDonations = _db.ClubDonations.Any(d => d.FriendlyClubId == attached.Id);
        if (hasDonations)
        {
            attached.IsActive = false;  // 改軟刪
            if (!_db.TrySaveChanges(out var err))
            {
                StatusMessage = $"軟刪失敗: {err}";
                return;
            }
            StatusMessage = $"{attached.ClubName} 有捐款紀錄,改軟刪 (IsActive=false)";
            Reload();
            return;
        }
        _db.FriendlyClubs.Remove(attached);
        if (!_db.TrySaveChanges(out var error))
        {
            StatusMessage = $"刪除失敗: {error}";
            return;
        }
        StatusMessage = $"已刪除 {attached.ClubName}";
        Selected = null;
        Reload();
    }

    /// <summary>子表:對目前選中友社新增一筆捐款記錄。</summary>
    [RelayCommand]
    private void AddDonation()
    {
        if (Selected is null) { StatusMessage = "先選一筆友社"; return; }
        if (NewDonationAmount <= 0) { StatusMessage = "金額必須 > 0"; return; }

        var donation = new ClubDonation
        {
            TxDate = NewDonationDate,
            FriendlyClubId = Selected.Id,
            FriendlyClubName = Selected.ClubName,
            Direction = NewDonationDirection,
            Amount = NewDonationAmount,
            Purpose = NewDonationPurpose,
        };
        _db.ClubDonations.Add(donation);
        if (!_db.TrySaveChanges(out var error))
        {
            StatusMessage = $"捐款新增失敗: {error}";
            return;
        }
        StatusMessage = $"已新增 {(donation.Direction == DonationDirection.Out ? "付" : "收")} {Selected.ClubName} ${donation.Amount:N0}";
        NewDonationAmount = 0m;
        ReloadDonations();
    }

    [RelayCommand]
    private void DeleteDonation()
    {
        if (SelectedDonation is null) { StatusMessage = "先選一筆捐款"; return; }
        var attached = _db.ClubDonations.FirstOrDefault(d => d.Id == SelectedDonation.Id);
        if (attached is null) return;
        _db.ClubDonations.Remove(attached);
        if (!_db.TrySaveChanges(out var error))
        {
            StatusMessage = $"刪除失敗: {error}";
            return;
        }
        ReloadDonations();
        StatusMessage = $"已刪除捐款 #{attached.Id}";
    }

    [RelayCommand]
    private void ExportDonationsCsv()
    {
        try
        {
            var safeName = Selected?.ClubCode ?? "ALL";
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                $"HsRotaryClub_友社捐款_{safeName}.csv");
            CsvExporter.WriteCsv(path, Donations.ToList());
            StatusMessage = $"已匯出 {Donations.Count} 筆 → {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"匯出失敗: {ex.Message}";
        }
    }
}
