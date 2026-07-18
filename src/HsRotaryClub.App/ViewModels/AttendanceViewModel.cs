using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HsRotaryClub.App.Infrastructure;
using HsRotaryClub.Domain;
using HsRotaryClub.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HsRotaryClub.App.ViewModels;

/// <summary>
/// v0.11 — M4 社友例會出席管理。
/// 對齊舊版畫面 「(3)年度出席資料記錄檔」 + 「(2)年度組別資料對照」。
/// 上半: 年度組別 (GroupLeader / GroupMember + 出席率)
/// 下半: 例會出缺席記錄 (MeetingDate × Member × Type)
/// </summary>
public partial class AttendanceViewModel : ObservableObject
{
    private readonly RotaryDbContext _db;
    private readonly CurrentClubContext _currentClub;

    public ObservableCollection<AttendanceGroup> Groups { get; } = new();
    public ObservableCollection<AttendanceRecord> Records { get; } = new();
    public ObservableCollection<int> Years { get; } = new();

    [ObservableProperty]
    private AttendanceGroup? _selectedGroup;

    [ObservableProperty]
    private int _year = DateTime.Today.Year;

    [ObservableProperty]
    private string _statusMessage = "就緒";

    public AttendanceViewModel(RotaryDbContext db, CurrentClubContext currentClub)
    {
        _db = db;
        _currentClub = currentClub;
        LoadYears();
        Reload();
        _currentClub.CurrentClubIdChanged += (_, _) => Reload();
    }

    private void LoadYears()
    {
        Years.Clear();
        // 從 db 拿所有出現過的年度,加上當年 ± 2
        var yearList = new HashSet<int> { DateTime.Today.Year - 1, DateTime.Today.Year, DateTime.Today.Year + 1 };
        var dbYears = _db.AttendanceRecords.AsNoTracking().Select(r => r.Year).Distinct().ToList();
        var grpYears = _db.AttendanceGroups.AsNoTracking().Select(g => g.Year).Distinct().ToList();
        foreach (var y in dbYears.Concat(grpYears)) yearList.Add(y);
        foreach (var y in yearList.OrderByDescending(y => y))
            Years.Add(y);
    }

    partial void OnYearChanged(int value) => Reload();
    partial void OnSelectedGroupChanged(AttendanceGroup? value) => ReloadRecords();

    [RelayCommand]
    private void Reload()
    {
        Groups.Clear();
        var q = _db.AttendanceGroups.AsNoTracking()
            .Where(g => g.ClubId == _currentClub.CurrentClubId && g.Year == Year);
        foreach (var g in q.OrderBy(g => g.GroupName).ToList())
            Groups.Add(g);
        SelectedGroup ??= Groups.FirstOrDefault();
        ReloadRecords();
        StatusMessage = $"載入 {Groups.Count} 個組別 ({Year})";
    }

    [RelayCommand]
    private void ReloadRecords()
    {
        Records.Clear();
        if (SelectedGroup is null) return;

        var leaderCode = SelectedGroup.GroupLeaderCode;
        var memberCode = SelectedGroup.GroupMemberCode;

        // 取組長 + 組員的當年出席記錄
        var q = _db.AttendanceRecords.AsNoTracking()
            .Where(r => r.ClubId == _currentClub.CurrentClubId
                && r.Year == Year
                && (r.MemberCode == leaderCode || r.MemberCode == memberCode))
            .OrderBy(r => r.MeetingDate);
        foreach (var r in q.ToList())
            Records.Add(r);
        StatusMessage = $"組別 {SelectedGroup.GroupName}: {Records.Count} 筆出席記錄";
    }

    [RelayCommand]
    private void AddGroup()
    {
        var g = new AttendanceGroup
        {
            ClubId = _currentClub.CurrentClubId,
            Year = Year,
            GroupName = $"新組{Groups.Count + 1}",
            IsActive = true,
            ShouldAttend = 10,
            ActualAttend = 0,
            MakeupAttend = 0,
        };
        _db.AttendanceGroups.Add(g);
        if (!_db.TrySaveChanges(out var err))
        {
            StatusMessage = $"新增失敗: {err}";
            return;
        }
        Reload();
        SelectedGroup = Groups.FirstOrDefault(x => x.Id == g.Id);
    }

    [RelayCommand]
    private void SaveGroup()
    {
        if (SelectedGroup is null) return;
        var attached = _db.AttendanceGroups.FirstOrDefault(g => g.Id == SelectedGroup.Id);
        if (attached is null)
        {
            StatusMessage = "DB 找不到此組別";
            return;
        }
        _db.Entry(attached).CurrentValues.SetValues(SelectedGroup);
        if (!_db.TrySaveChanges(out var err))
        {
            StatusMessage = $"儲存失敗: {err}";
            return;
        }
        StatusMessage = $"已儲存組別 {attached.GroupName}";
        Reload();
    }

    [RelayCommand]
    private void DeactivateGroup()
    {
        if (SelectedGroup is null) return;
        var attached = _db.AttendanceGroups.FirstOrDefault(g => g.Id == SelectedGroup.Id);
        if (attached is null) return;
        attached.IsActive = false;
        if (!_db.TrySaveChanges(out var err))
        {
            StatusMessage = $"停用失敗: {err}";
            return;
        }
        StatusMessage = $"已停用組別 {attached.GroupName}";
        Reload();
    }

    /// <summary>計算當前組別的出席率 (0~1)。</summary>
    public double SelectedGroupRate =>
        SelectedGroup is null || SelectedGroup.ShouldAttend == 0
            ? 0
            : (SelectedGroup.ActualAttend + SelectedGroup.MakeupAttend) / (double)SelectedGroup.ShouldAttend;

    /// <summary>出席率文字 (例如 "90.0%")。</summary>
    public string SelectedGroupRateText => $"{SelectedGroupRate:P1}";
}