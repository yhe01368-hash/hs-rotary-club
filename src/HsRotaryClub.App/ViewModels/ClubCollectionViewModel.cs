using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HsRotaryClub.App.Controls;
using HsRotaryClub.App.Infrastructure;
using HsRotaryClub.Domain;
using HsRotaryClub.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HsRotaryClub.App.ViewModels;

public partial class ClubCollectionViewModel : ObservableObject
{
    private readonly RotaryDbContext _db;
    private readonly CurrentClubContext _currentClub;

    public ObservableCollection<ClubCollection> Collections { get; } = new();
    public ObservableCollection<MonthlyReceivableSpec> ReceivableSpecs { get; } = new();

    [ObservableProperty]
    private ClubCollection? _selected;

    [ObservableProperty]
    private int _year = DateTime.Today.Year;

    [ObservableProperty]
    private int _month = DateTime.Today.Month;

    [ObservableProperty]
    private string _filter = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "就緒";

    public ClubCollectionViewModel(RotaryDbContext db, CurrentClubContext currentClub)
    {
        _db = db;
        _currentClub = currentClub;
        Reload();
        _currentClub.CurrentClubIdChanged += (_, _) => Reload();
    }

    partial void OnYearChanged(int value) => Reload();
    partial void OnMonthChanged(int value) => Reload();
    partial void OnFilterChanged(string value) => Reload();

    [RelayCommand]
    private void Reload()
    {
        var q = _db.ClubCollections.AsNoTracking().AsQueryable();
        q = q.Where(c => c.ClubId == _currentClub.CurrentClubId);  // v0.7 A5
        q = q.Where(c => c.Year == Year && c.Month == Month);
        if (!string.IsNullOrWhiteSpace(Filter))
        {
            q = q.Where(c =>
                c.MemberName.Contains(Filter) ||
                c.Category.Contains(Filter) ||
                c.MemberCode.ToString().Contains(Filter));
        }

        Collections.Clear();
        foreach (var c in q.OrderBy(c => c.CollectionDate).ToList())
            Collections.Add(c);
        Selected = Collections.FirstOrDefault();

        // 月度應收 (同年月) — v0.7 A5 加 club filter
        var specs = _db.MonthlyReceivableSpecs
            .AsNoTracking()
            .Where(s => s.ClubId == _currentClub.CurrentClubId && s.Year == Year && s.Month == Month)
            .OrderBy(s => s.MemberCode)
            .ToList();
        ReceivableSpecs.Clear();
        foreach (var s in specs) ReceivableSpecs.Add(s);

        StatusMessage = $"載入 {Collections.Count} 筆收款 / {ReceivableSpecs.Count} 筆應收 ({Year}-{Month:D2})";
    }

    [RelayCommand]
    private void Add()
    {
        var c = new ClubCollection
        {
            Year = Year, Month = Month,
            CollectionDate = new DateOnly(Year, Month, 1),
            Category = "會費",
            MemberCode = 0,
            MemberName = "(待選)", // v0.33: 設為 "(待選)" 顯示後,由 user 從右側選社員填入.
            ClubId = _currentClub.CurrentClubId,  // v0.7 A5
        };
        _db.ClubCollections.Add(c);
        if (!_db.TrySaveChanges(out var error))
        {
            StatusMessage = $"新增失敗: {error}";
            return;
        }
        // v0.33: SaveChanges 已給 EF 自動 Id. Reload 後 Selected 會是剛加的 row,
        // 為讓 user 看見 row 識別,把 MemberName 加上 "#Id" 前綴直到真的選社員.
        c.MemberName = $"#{c.Id} (待選)";
        if (!_db.TrySaveChanges(out var err2))
        {
            // 改 prefix 失敗不影響主新增 — 主 row 已存.
            StatusMessage = $"新增成功但 id 顯示失敗: {err2}";
        }
        Reload();
        Selected = Collections.FirstOrDefault(x => x.Id == c.Id);
        StatusMessage = $"已新增 #{c.Id} (待選社員),請從右側選社員";
    }

    [RelayCommand]
    private void Save()
    {
        if (Selected is null) { StatusMessage = "沒有選到"; return; }
        var attached = _db.ClubCollections.FirstOrDefault(c => c.Id == Selected.Id);
        if (attached is null) { StatusMessage = "DB 找不到"; return; }
        _db.Entry(attached).CurrentValues.SetValues(Selected);
        if (!_db.TrySaveChanges(out var error))
        {
            StatusMessage = $"儲存失敗: {error}";
            return;
        }
        StatusMessage = $"已儲存 #{attached.Id}";
    }

    [RelayCommand]
    private void Delete()
    {
        if (Selected is null) { StatusMessage = "沒有選到"; return; }
        var attached = _db.ClubCollections.FirstOrDefault(c => c.Id == Selected.Id);
        if (attached is null) { StatusMessage = "DB 找不到"; return; }
        _db.ClubCollections.Remove(attached);
        if (!_db.TrySaveChanges(out var error))
        {
            StatusMessage = $"刪除失敗: {error}";
            return;
        }
        StatusMessage = $"已刪除 #{attached.Id}";
        Reload();
    }

    [RelayCommand]
    private void PickMember()
    {
        var picked = MemberLookupDialog.Ask();
        if (picked is null || Selected is null) return;
        // v0.31: 對 Selected 重新 set 一次觸發 INPC,確保欄位 binding 重新讀取.
        // sub-property 寫入 (Selected.MemberCode = ...) 不會自動 fire INPC,
        // 因為 Selected reference 沒變,所以右側欄位 + grid row 都不 refresh.
        // 用 local 暫存 → set null → 改 sub property → restore 強制 INPC.
        var orig = Selected;
        Selected = null;
        orig.MemberCode = picked.Code;
        orig.MemberName = picked.Name;
        Selected = orig;
        StatusMessage = $"已綁社員: {picked.Code} {picked.Name}";
    }

    [RelayCommand]
    private void ExportCsv()
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                $"HsRotaryClub_收款_{Year}-{Month:D2}.csv");
            CsvExporter.WriteCsv(path, Collections.ToList());
            StatusMessage = $"已匯出 {Collections.Count} 筆 → {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"匯出失敗: {ex.Message}";
        }
    }

    /// <summary>針對選到的 spec 自動算出目前已收 (sum 同月同社員同項目) — 給儲存按鈕用。</summary>
    [RelayCommand]
    private void RecomputeSpec()
    {
        if (Selected is null) { StatusMessage = "先選一筆收款"; return; }
        var spec = ReceivableSpecs.FirstOrDefault(s =>
            s.MemberCode == Selected.MemberCode &&
            Year == s.Year && Month == s.Month);
        if (spec is null)
        {
            StatusMessage = "找不到對應的應收項目 (ReceivableSpec)";
            return;
        }
        spec.SettledAmount = Collections
            .Where(c => c.MemberCode == Selected.MemberCode && c.Category == spec.Item)
            .Sum(c => c.CashAmount + c.CheckAmount);
        _db.MonthlyReceivableSpecs.Update(spec);
        if (!_db.TrySaveChanges(out var error))
        {
            StatusMessage = $"Recompute 儲存失敗: {error}";
            return;
        }
        StatusMessage = $"#{spec.MemberCode} {spec.Item} 已收 → {spec.SettledAmount} / {spec.Amount}";
        Reload();
    }
}
