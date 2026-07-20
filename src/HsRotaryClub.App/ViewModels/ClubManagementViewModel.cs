using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HsRotaryClub.App.Controls;
using HsRotaryClub.App.Infrastructure;
using HsRotaryClub.Domain;
using HsRotaryClub.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HsRotaryClub.App.ViewModels;

/// <summary>
/// 社團管理 — v0.7 A3 + A5。
/// 對齊舊版畫面 01 「會員資料管理作業」的「社團」面,但這版先純 Club entity CRUD + soft-delete。
/// MakeCurrent 切 CurrentClubContext — 推播給所有 VM 自動 Reload。
/// </summary>
public partial class ClubManagementViewModel : ObservableObject
{
    private readonly RotaryDbContext _db;
    private readonly CurrentClubContext _currentClubCtx;

    public ObservableCollection<Club> Clubs { get; } = new();

    [ObservableProperty]
    private Club? _selected;

    [ObservableProperty]
    private string _filter = string.Empty;

    [ObservableProperty]
    private bool _showInactiveOnly;

    [ObservableProperty]
    private string _statusMessage = "就緒";

    /// <summary>app 啟動時選定的社 ID;UI 操作切社後存這裡給其他 VM filter 用。</summary>
    [ObservableProperty]
    private int _currentClubId = SeedData.DefaultClubId;

    [ObservableProperty]
    private string _currentClubName = "";

    public ClubManagementViewModel(RotaryDbContext db, CurrentClubContext currentClubCtx)
    {
        _db = db;
        _currentClubCtx = currentClubCtx;
        Reload();
    }

    partial void OnFilterChanged(string value) => Reload();
    partial void OnShowInactiveOnlyChanged(bool value) => Reload();

    [RelayCommand]
    private void Reload()
    {
        try
        {
            DoReload();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Reload 失敗: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[ClubMgmt.Reload] {ex}");
        }
    }

    private void DoReload()
    {
        Clubs.Clear();
        var q = _db.Clubs.AsNoTracking().AsQueryable();
        q = ShowInactiveOnly ? q.Where(c => !c.IsActive) : q.Where(c => c.IsActive);
        if (!string.IsNullOrWhiteSpace(Filter))
        {
            q = q.Where(c => c.Name.Contains(Filter) || (c.District ?? "").Contains(Filter));
        }
        foreach (var c in q.OrderBy(c => c.Name).ToList())
            Clubs.Add(c);
        Selected ??= Clubs.FirstOrDefault(c => c.Id == _currentClubCtx.CurrentClubId) ?? Clubs.FirstOrDefault();
        CurrentClubName = _currentClubCtx.CurrentClubName;
        StatusMessage = $"載入 {Clubs.Count} 個社團";
    }

    [RelayCommand]
    private void Add()
    {
        var c = new Club
        {
            Name = "新社",
            District = "",
            IsActive = true,
        };
        _db.Clubs.Add(c);
        if (!_db.TrySaveChanges(out var error))
        {
            StatusMessage = $"新增失敗: {error}";
            return;
        }
        Reload();
        Selected = Clubs.FirstOrDefault(x => x.Id == c.Id);
    }

    [RelayCommand]
    private void Save()
    {
        if (Selected is null) { StatusMessage = "沒有選到社團"; return; }
        var attached = _db.Clubs.FirstOrDefault(c => c.Id == Selected.Id);
        if (attached is null) { StatusMessage = "DB 找不到"; return; }
        _db.Entry(attached).CurrentValues.SetValues(Selected);
        if (!_db.TrySaveChanges(out var error))
        {
            StatusMessage = $"儲存失敗: {error}";
            return;
        }
        StatusMessage = $"已儲存 {attached.Name}";
        Reload();
    }

    /// <summary>軟刪: IsActive=false。資料保留給未來 v0.11 還原工具。</summary>
    [RelayCommand]
    private void Deactivate()
    {
        if (Selected is null) return;
        var attached = _db.Clubs.FirstOrDefault(c => c.Id == Selected.Id);
        if (attached is null) return;
        attached.IsActive = false;
        if (!_db.TrySaveChanges(out var error))
        {
            StatusMessage = $"軟刪失敗: {error}";
            return;
        }
        StatusMessage = $"已停用 {attached.Name}";
        Reload();
    }

    /// <summary>切換 current club (切到 Selected 為操作社) — v0.7 A5 推播給其他 VM。</summary>
    [RelayCommand]
    private void MakeCurrent()
    {
        if (Selected is null) return;
        _currentClubCtx.SetCurrent(Selected.Id, Selected.Name);
        StatusMessage = $"已切到「{Selected.Name}」為操作社";
    }

    /// <summary>v0.8 — 拉 ImportExport dialog。</summary>
    [RelayCommand]
    private void ImportExport()
    {
        var owner = System.Windows.Application.Current?.MainWindow;
        ImportExportDialog.Show(_db, _currentClubCtx.CurrentClubId, _currentClubCtx.CurrentClubName, owner);
    }

    /// <summary>v0.15 — 拉 Migration dialog (從舊版 mdb 遷移)。</summary>
    [RelayCommand]
    private void MigrateFromMdb()
    {
        var owner = System.Windows.Application.Current?.MainWindow;
        HsRotaryClub.App.Controls.MigrationDialog.Show(_db, owner);
    }
}
