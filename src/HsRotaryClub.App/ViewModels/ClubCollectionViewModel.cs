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
    private readonly CurrentUserContext _currentUser;  // v0.40: 自動帶入當前 user 作為收款人

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

    /// <summary>
    /// v0.35: 收款日期獨立屬性. DatePicker 綁這,不綁 Selected.CollectionDate.
    /// 否則 user 改 DatePicker 寫到舊 Selected 上,Save 後舊 row 被覆寫;Add() 看 Selected 還是舊日期.
    /// </summary>
    [ObservableProperty]
    private DateTime? _newDate = DateTime.Today;

    public ClubCollectionViewModel(RotaryDbContext db, CurrentClubContext currentClub, CurrentUserContext currentUser)
    {
        _db = db;
        _currentClub = currentClub;
        _currentUser = currentUser;  // v0.40
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
        // v0.34: 若 user 已在右側編輯 (Selected) 並輸入 社員/金額/日期,Add 直接複用,
        // 不要 hardcode placeholder. 沒 Selected 才退到 (待選) placeholder.
        var src = Selected;
        var c = new ClubCollection
        {
            Year = Year, Month = Month,
            // v0.35: 從獨立 NewDate 屬性取日期,不從 Selected.CollectionDate (避免寫入舊 row).
            CollectionDate = NewDate.HasValue ? DateOnly.FromDateTime(NewDate.Value) : new DateOnly(Year, Month, 1),
            Category = string.IsNullOrWhiteSpace(src?.Category) ? "會費" : src!.Category,
            MemberCode = src?.MemberCode ?? 0,
            MemberName = src?.MemberName ?? "(待選)",
            CashAmount = src?.CashAmount ?? 0m,
            CheckAmount = src?.CheckAmount ?? 0m,
            ReceiptNo = src?.ReceiptNo ?? "",
            // v0.41.1: 收款人預設 = 當前登入 user (DisplayName). 用 CurrentDisplayName 是否非空當 guard
            // (不要依賴 IsAuthenticated,因為 LoginDialog 是在 MainWindow 建構後才設值,VM 早期可能還讀到 false).
            Collector = !string.IsNullOrWhiteSpace(src?.Collector)
                ? src!.Collector
                : (_currentUser.CurrentDisplayName ?? ""),
            ClubId = _currentClub.CurrentClubId,  // v0.7 A5
        };
        _db.ClubCollections.Add(c);
        if (!_db.TrySaveChanges(out var error))
        {
            StatusMessage = $"新增失敗: {error}";
            return;
        }
        // v0.33: SaveChanges 已給 EF 自動 Id. 當 MemberName 仍是 placeholder 時顯示 row id.
        if (string.IsNullOrEmpty(src?.MemberName) || src!.MemberName == "(待選)")
        {
            c.MemberName = $"#{c.Id} (待選)";
            if (!_db.TrySaveChanges(out var err2))
            {
                StatusMessage = $"新增成功但 id 顯示失敗: {err2}";
            }
        }
        Reload();
        Selected = Collections.FirstOrDefault(x => x.Id == c.Id);
        StatusMessage = $"已新增 #{c.Id} {c.MemberName}";
    }

    [RelayCommand]
    private void Save()
    {
        if (Selected is null) { StatusMessage = "沒有選到"; return; }
        var attached = _db.ClubCollections.FirstOrDefault(c => c.Id == Selected.Id);
        if (attached is null) { StatusMessage = "DB 找不到"; return; }
        _db.Entry(attached).CurrentValues.SetValues(Selected);
        // v0.41: Save 時若 Collector 空 → 自動帶入當前 user (補回 user 不透過 Add 而是直接編輯舊 row).
        // v0.41.1: 改用 CurrentDisplayName guard (不用 IsAuthenticated).
        if (string.IsNullOrWhiteSpace(attached.Collector) && !string.IsNullOrEmpty(_currentUser.CurrentDisplayName))
        {
            attached.Collector = _currentUser.CurrentDisplayName;
        }
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
        if (picked is null) return;
        // v0.36: 沒有 Selected 時,自動 Add() 創一個 placeholder row 給 Selected 綁定,
        // 這樣 user 按「挑選社員」就不會 silently 被 Selected null guard 擋掉.
        if (Selected is null)
        {
            Add();
            if (Selected is null)
            {
                StatusMessage = "無法建立新 row,請先按「新增」";
                return;
            }
        }
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
