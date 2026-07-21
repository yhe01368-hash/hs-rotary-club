using CommunityToolkit.Mvvm.ComponentModel;
using HsRotaryClub.Domain;

namespace HsRotaryClub.App.Infrastructure;

/// <summary>
/// v0.38 — 當前登入用戶 context (DI singleton).
/// 設值後所有 ViewModel / Audit / Collector 欄位可讀.
/// 由 LoginDialog 登入成功後呼叫 SetCurrent.
/// </summary>
public partial class CurrentUserContext : ObservableObject
{
    [ObservableProperty]
    private int _currentUserId;

    [ObservableProperty]
    private string _currentUsername = "";

    [ObservableProperty]
    private string _currentDisplayName = "";

    [ObservableProperty]
    private UserRole _currentRole = UserRole.Member;

    [ObservableProperty]
    private bool _isAuthenticated;

    public void SetCurrent(User user)
    {
        CurrentUserId = user.Id;
        CurrentUsername = user.Username;
        CurrentDisplayName = user.DisplayName;
        CurrentRole = user.Role;
        IsAuthenticated = true;
    }

    public void Clear()
    {
        CurrentUserId = 0;
        CurrentUsername = "";
        CurrentDisplayName = "";
        CurrentRole = UserRole.Member;
        IsAuthenticated = false;
    }
}
