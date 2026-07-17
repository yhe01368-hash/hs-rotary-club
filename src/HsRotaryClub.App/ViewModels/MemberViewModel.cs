using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HsRotaryClub.Domain;
using HsRotaryClub.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HsRotaryClub.App.ViewModels;

/// <summary>
/// M1 社員資料維護 — 完整版。
/// 對應舊版 「社員基本資料維護」畫面 (TS81.mdb)。
/// 左側速查清單 + 右側編輯表單 + 「現任社員 / 顯示刪除社員」toggle。
/// </summary>
public partial class MemberViewModel : ObservableObject
{
    private readonly RotaryDbContext _db;

    public ObservableCollection<Member> Members { get; } = new();

    [ObservableProperty]
    private Member? _selected;

    [ObservableProperty]
    private string _filter = string.Empty;

    /// <summary>true = 顯示現任社員 (default),false = 顯示已遷出 / 軟刪社員</summary>
    [ObservableProperty]
    private bool _showCurrentOnly = true;

    [ObservableProperty]
    private string _statusMessage = "就緒";

    public MemberViewModel(RotaryDbContext db)
    {
        _db = db;
        Reload();
    }

    partial void OnFilterChanged(string value) => Reload();
    partial void OnShowCurrentOnlyChanged(bool value)
    {
        Reload();
        StatusMessage = ShowCurrentOnly ? "現任社員" : "已遷出 / 刪除社員";
    }

    [RelayCommand]
    private void Reload()
    {
        Members.Clear();
        var q = _db.Members.AsNoTracking().AsQueryable();
        q = ShowCurrentOnly ? q.Where(m => m.IsCurrent) : q.Where(m => !m.IsCurrent);
        if (!string.IsNullOrWhiteSpace(Filter))
        {
            q = q.Where(m =>
                m.Name.Contains(Filter) ||
                (m.EnglishName != null && m.EnglishName.Contains(Filter)) ||
                m.Code.ToString().Contains(Filter));
        }
        foreach (var m in q.OrderBy(m => m.Code).ToList())
        {
            Members.Add(m);
        }
        Selected ??= Members.FirstOrDefault();
        StatusMessage = $"載入 {Members.Count} 筆 ({(ShowCurrentOnly ? "現任" : "離社")})";
    }

    /// <summary>「修正資料」按鈕 — 寫回 DB。</summary>
    [RelayCommand]
    private void Save()
    {
        if (Selected is null) { StatusMessage = "沒有選到社員"; return; }

        // 用 Live context (AsNoTracking 在 Reload 後 entity 是 detached) —
        // 直接 Attach + State Modified 對單一欄位容易踩到 not-mapped,
        // 乾淨的做法: Attachable clone 進 attached instance
        var attached = _db.Members.FirstOrDefault(m => m.Id == Selected.Id);
        if (attached is null)
        {
            StatusMessage = "DB 找不到此社員 (已被刪?)";
            return;
        }
        _db.Entry(attached).CurrentValues.SetValues(Selected);
        Validate(attached);
        if (!_db.TrySaveChanges(out var error))
        {
            StatusMessage = $"儲存失敗: {error}";
            return;
        }
        StatusMessage = $"已儲存 {attached.Name} (#{attached.Code})";
        Reload();
    }

    /// <summary>「新增資料」按鈕 — 建空社員 + 直接編輯。</summary>
    [RelayCommand]
    private void Add()
    {
        var nextCode = (_db.Members.Max(m => (int?)m.Code) ?? 0) + 1;
        var m = new Member
        {
            Code = nextCode,
            Name = "新社員",
            EnglishName = null,
            IsCurrent = true,
        };
        _db.Members.Add(m);
        _db.SaveChanges();
        Reload();
        Selected = Members.FirstOrDefault(x => x.Id == m.Id) ?? (object?)m as Member;
        StatusMessage = $"已新增 {m.Name} (#{m.Code}),請編輯後按「修正資料」儲存";
    }

    /// <summary>「刪除資料」按鈕 — 軟刪 (IsCurrent=false) + 填遷出日。</summary>
    [RelayCommand]
    private void Delete()
    {
        if (Selected is null) { StatusMessage = "沒有選到社員"; return; }
        var attached = _db.Members.FirstOrDefault(m => m.Id == Selected.Id);
        if (attached is null) { StatusMessage = "DB 找不到此社員"; return; }

        attached.IsCurrent = false;
        attached.LeaveDate ??= DateOnly.FromDateTime(DateTime.Today);
        attached.LeaveReason ??= "軟刪";
        _db.SaveChanges();
        StatusMessage = $"已軟刪 {attached.Name} (#{attached.Code})";
        Selected = null;
        Reload();
    }

    /// <summary>「社員選出」按鈕 — 隱含行為 = 鎖定選中列 (打補丁用)。</summary>
    [RelayCommand]
    private void Pin() => StatusMessage = Selected is null ? "未選社員" : $"已選出 {Selected.Name}";

    private static IEnumerable<string> Validate(Member m)
    {
        var ctx = new ValidationContext(m);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(m, ctx, results, validateAllProperties: true);
        return results.Select(r => r.ErrorMessage ?? "驗證失敗");
    }
}
