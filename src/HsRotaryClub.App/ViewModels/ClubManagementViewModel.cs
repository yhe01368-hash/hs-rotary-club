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
/// v0.7 A3 + A5。
/// MakeCurrent 切 CurrentClubContext — 推播給所有 VM 自動 Reload.
/// v0.29: 自動遷移舊 seed 名「豐原西南扶輪社」→「示範扶輪社」避免跟用戶新建社撞 UNIQUE.
/// </summary>
public partial class ClubManagementViewModel : ObservableObject
{
    private const string LegacySeedName = "豐原西南扶輪社";
    private const string GenericSeedName = "示範扶輪社";

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

    /// <summary>
    /// v0.29 — 自動把舊 demo seed name 改成通用名。
    /// 這樣 user 新增同名社團時不會撞 Clubs.Name UNIQUE constraint.
    /// </summary>
    private void MigrateLegacySeedNames()
    {
        try
        {
            var targets = _db.Clubs.Where(c => c.Name == LegacySeedName).ToList();
            if (targets.Count == 0) return;
            foreach (var c in targets)
            {
                c.Name = GenericSeedName;
            }
            if (_db.TrySaveChanges(out var error))
            {
                System.Diagnostics.Debug.WriteLine($"[v0.29] migrated {targets.Count} clubs from '{LegacySeedName}' to '{GenericSeedName}'");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[v0.29] migrate failed: {error}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[v0.29] migrate exception: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Reload()
    {
        try
        {
            MigrateLegacySeedNames();
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
        catch (Exception ex)
        {
            StatusMessage = $"Reload 失敗: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[ClubMgmt.Reload] {ex}");
        }
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
        // v0.54: 儲存後清掉 Selected,避免下一次操作覆蓋剛儲存或誤切到 seed 那筆
        Selected = null;
        StatusMessage = $"已儲存 {attached.Name},可繼續編輯或新增";
    }

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

    [RelayCommand]
    private void MakeCurrent()
    {
        if (Selected is null) return;
        _currentClubCtx.SetCurrent(Selected.Id, Selected.Name);
        StatusMessage = $"已切到「{Selected.Name}」為操作社";
    }

    [RelayCommand]
    private void ImportExport()
    {
        var owner = System.Windows.Application.Current?.MainWindow;
        ImportExportDialog.Show(_db, _currentClubCtx.CurrentClubId, _currentClubCtx.CurrentClubName, owner);
    }

    [RelayCommand]
    private void MigrateFromMdb()
    {
        var owner = System.Windows.Application.Current?.MainWindow;
        HsRotaryClub.App.Controls.MigrationDialog.Show(_db, owner);
    }
}
