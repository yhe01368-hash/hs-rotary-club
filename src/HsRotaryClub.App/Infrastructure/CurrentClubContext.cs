using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HsRotaryClub.App.Infrastructure;

/// <summary>
/// v0.7 A5 — 全 app 共用的「當前操作社」狀態。
/// 由 ClubManagementViewModel.MakeCurrent 切,Subscription 推到 Member / Collection / FriendlyClub。
/// 所有 filter VM 訂閱 CurrentClubIdChanged → 自動 Reload。
/// </summary>
public partial class CurrentClubContext : ObservableObject
{
    [ObservableProperty]
    private int _currentClubId = HsRotaryClub.Domain.ClubDefaults.DefaultClubId;

    [ObservableProperty]
    private string _currentClubName = "";

    public event PropertyChangedEventHandler? CurrentClubIdChanged;

    partial void OnCurrentClubIdChanged(int value)
    {
        CurrentClubIdChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentClubId)));
    }

    public void SetCurrent(int id, string name)
    {
        CurrentClubId = id;
        CurrentClubName = name;
    }
}
