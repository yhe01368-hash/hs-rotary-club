using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HsRotaryClub.App.Infrastructure;
using HsRotaryClub.Domain;
using HsRotaryClub.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HsRotaryClub.App.ViewModels;

/// <summary>
/// v0.13 — M7 信件作業 (對應舊版 「(7)各種信件作業系統」)。
/// 上: 信件清單 (MailJob)
/// 下: 該封信的收件人清單 + 寄送狀態
/// </summary>
public partial class MailViewModel : ObservableObject
{
    private readonly RotaryDbContext _db;
    private readonly CurrentClubContext _currentClub;

    public ObservableCollection<MailJob> Jobs { get; } = new();
    public ObservableCollection<MailRecipient> Recipients { get; } = new();

    [ObservableProperty]
    private MailJob? _selectedJob;

    [ObservableProperty]
    private string _statusMessage = "就緒";

    public MailViewModel(RotaryDbContext db, CurrentClubContext currentClub)
    {
        _db = db;
        _currentClub = currentClub;
        Reload();
        _currentClub.CurrentClubIdChanged += (_, _) => Reload();
    }

    partial void OnSelectedJobChanged(MailJob? value) => ReloadRecipients();

    [RelayCommand]
    private void Reload()
    {
        Jobs.Clear();
        var q = _db.MailJobs.AsNoTracking()
            .Where(j => j.ClubId == _currentClub.CurrentClubId);
        foreach (var j in q.OrderByDescending(j => j.CreatedAt).ToList())
            Jobs.Add(j);
        SelectedJob ??= Jobs.FirstOrDefault();
        ReloadRecipients();
        StatusMessage = $"載入 {Jobs.Count} 封信";
    }

    [RelayCommand]
    private void ReloadRecipients()
    {
        Recipients.Clear();
        if (SelectedJob is null) return;
        var q = _db.MailRecipients.AsNoTracking()
            .Where(r => r.MailJobId == SelectedJob.Id);
        foreach (var r in q.OrderBy(r => r.MemberCode).ToList())
            Recipients.Add(r);
    }

    [RelayCommand]
    private void AddJob()
    {
        var j = new MailJob
        {
            ClubId = _currentClub.CurrentClubId,
            Subject = "新信件",
            Content = "",
            ScheduleType = "OneOff",
        };
        _db.MailJobs.Add(j);
        if (!_db.TrySaveChanges(out var err))
        {
            StatusMessage = $"新增失敗: {err}";
            return;
        }
        Reload();
        SelectedJob = Jobs.FirstOrDefault(x => x.Id == j.Id);
    }

    [RelayCommand]
    private void SaveJob()
    {
        if (SelectedJob is null) return;
        var attached = _db.MailJobs.FirstOrDefault(j => j.Id == SelectedJob.Id);
        if (attached is null) return;
        _db.Entry(attached).CurrentValues.SetValues(SelectedJob);
        if (!_db.TrySaveChanges(out var err))
        {
            StatusMessage = $"儲存失敗: {err}";
            return;
        }
        StatusMessage = $"已儲存信件 {attached.Subject}";
        Reload();
    }

    [RelayCommand]
    private void DeleteJob()
    {
        if (SelectedJob is null) return;
        // 先刪收件人
        var recipients = _db.MailRecipients.Where(r => r.MailJobId == SelectedJob.Id).ToList();
        foreach (var r in recipients) _db.MailRecipients.Remove(r);
        _db.MailJobs.Remove(SelectedJob);
        if (!_db.TrySaveChanges(out var err))
        {
            StatusMessage = $"刪除失敗: {err}";
            return;
        }
        Reload();
    }

    [RelayCommand]
    private void AddRecipientsFromMembers()
    {
        if (SelectedJob is null)
        {
            StatusMessage = "先選一封信";
            return;
        }
        var members = _db.Members.AsNoTracking()
            .Where(m => m.ClubId == _currentClub.CurrentClubId && m.IsCurrent && m.Email != null && m.Email != "")
            .ToList();
        var existingCodes = _db.MailRecipients.AsNoTracking()
            .Where(r => r.MailJobId == SelectedJob.Id).Select(r => r.MemberCode).ToHashSet();
        int added = 0;
        foreach (var m in members.Where(m => !existingCodes.Contains(m.Code)))
        {
            _db.MailRecipients.Add(new MailRecipient
            {
                MailJobId = SelectedJob.Id,
                MemberCode = m.Code,
                MemberName = m.Name,
                Email = m.Email!,
                Status = MailSendStatus.Pending,
            });
            added++;
        }
        if (!_db.TrySaveChanges(out var err))
        {
            StatusMessage = $"新增收件人失敗: {err}";
            return;
        }
        StatusMessage = $"新增 {added} 位收件人";
        ReloadRecipients();
    }

    public int SentCount => Recipients.Count(r => r.Status == MailSendStatus.Sent);
    public int FailedCount => Recipients.Count(r => r.Status == MailSendStatus.Failed);
    public int PendingCount => Recipients.Count(r => r.Status == MailSendStatus.Pending);
}