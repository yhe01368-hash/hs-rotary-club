using System.ComponentModel.DataAnnotations;

namespace HsRotaryClub.Domain;

/// <summary>
/// v0.38 — App 登入帳號。每個 User 可設定 role (Admin / Treasurer / Member).
/// 不同社團可有多個 user,user 自己可以管理自己的會費但不一定管全社.
/// PasswordHash = PBKDF2 SHA256 (100k iter, salt-prefixed).
/// 由 Admin 預建帳號,user 自己不開放註冊.
/// </summary>
public class User
{
    public int Id { get; set; }

    [Display(Name = "使用者名稱")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// PBKDF2-SHA256 hash. Format: "{salt}:{hashBase64}" (SHA256 32 bytes base64-encoded).
    /// </summary>
    [Display(Name = "密碼雜湊")]
    public string PasswordHash { get; set; } = string.Empty;

    [Display(Name = "顯示名稱")]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Admin = full access (社團管理, 帳號管理). Treasurer = 收費操作. Member = 查詢.
    /// v0.38: 預設 Admin only,所有 user 都 full access.
    /// </summary>
    [Display(Name = "角色")]
    public UserRole Role { get; set; } = UserRole.Admin;

    [Display(Name = "啟用")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "建立時間")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "最後登入")]
    public DateTime? LastLoginAt { get; set; }
}

public enum UserRole
{
    Admin = 0,
    Treasurer = 1,
    Member = 2,
}
