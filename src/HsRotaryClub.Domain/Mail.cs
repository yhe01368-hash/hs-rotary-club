using System.ComponentModel.DataAnnotations;

namespace HsRotaryClub.Domain;

/// <summary>
/// v0.12 — M7 信件作業 (對應舊版 「(7)各種信件作業系統」)。
/// MailJob = 一封信 (主旨 + 內容 + 附件)
/// MailRecipient = 收件人清單 + 寄送狀態
/// </summary>
public class MailJob
{
    public int Id { get; set; }

    [Display(Name = "所屬社")]
    public int ClubId { get; set; } = ClubDefaults.DefaultClubId;

    [Display(Name = "主旨")]
    public string Subject { get; set; } = "";

    [Display(Name = "內容")]
    public string Content { get; set; } = "";

    /// <summary>附件路徑 (本地 file)</summary>
    [Display(Name = "附件路徑")]
    public string? AttachmentPath { get; set; }

    /// <summary>Annual / Monthly / OneOff</summary>
    [Display(Name = "排程類型")]
    public string ScheduleType { get; set; } = "OneOff";

    [Display(Name = "建立時間")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "上次寄送")]
    public DateTime? LastSentAt { get; set; }
}

public enum MailSendStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2,
}

public class MailRecipient
{
    public int Id { get; set; }

    [Display(Name = "MailJob")]
    public int MailJobId { get; set; }

    [Display(Name = "社員編號")]
    public int MemberCode { get; set; }

    [Display(Name = "社員姓名")]
    public string MemberName { get; set; } = "";

    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Display(Name = "寄送狀態")]
    public MailSendStatus Status { get; set; } = MailSendStatus.Pending;

    [Display(Name = "錯誤訊息")]
    public string? ErrorMessage { get; set; }

    [Display(Name = "寄送時間")]
    public DateTime? SentAt { get; set; }
}