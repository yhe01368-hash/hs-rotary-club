using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HsRotaryClub.App.Infrastructure;

namespace HsRotaryClub.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private const string AppName = "HsRotaryClub 社務行政系統";

    public ObservableCollection<NavItem> Modules { get; } = new();

    [ObservableProperty]
    private NavItem? _selected;

    [ObservableProperty]
    private object? _currentView;

    private string _currentClubName = "";

    /// <summary>
    /// 動態 window title: HsRotaryClub 社務行政系統 — [CurrentClubName].
    /// v0.28: 移除硬寫 示範扶輪社,改為跟著當前社動態變動.
    /// </summary>
    [ObservableProperty]
    private string _windowTitle = AppName;

    public MainWindowViewModel(
        HomeViewModel home,
        ClubManagementViewModel clubs,
        MemberViewModel member,
        ClubCollectionViewModel collection,
        FriendlyClubViewModel friendly,
        AttendanceViewModel attendance,
        OtherTransactionViewModel otherTxn,
        AccountingViewModel accounting,
        MailViewModel mail,
        CurrentClubContext currentClubCtx)
    {
        _currentClubName = currentClubCtx.CurrentClubName;
        currentClubCtx.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(CurrentClubContext.CurrentClubName))
            {
                _currentClubName = currentClubCtx.CurrentClubName;
                UpdateTitle();
            }
        };
        UpdateTitle();

        Modules.Add(new NavItem("🏠 首頁",     home,        "01"));
        Modules.Add(new NavItem("🏢 社團管理", clubs,       "02"));
        Modules.Add(new NavItem("👤 社員資料", member,      "03"));
        Modules.Add(new NavItem("💰 會內收款", collection,  "04"));
        Modules.Add(new NavItem("★ 友社捐款", friendly,    "05"));
        Modules.Add(new NavItem("◆ 例會出席", attendance,   "06"));
        Modules.Add(new NavItem("📒 其它收支", otherTxn,    "07"));
        Modules.Add(new NavItem("📊 會計月報", accounting,  "08"));
        Modules.Add(new NavItem("📧 信件作業", mail,        "09"));
        Selected = Modules[0];
        CurrentView = Selected.ViewModel;
    }

    private void UpdateTitle()
    {
        WindowTitle = string.IsNullOrWhiteSpace(_currentClubName)
            ? AppName
            : $"{_currentClubName} — {AppName}";
    }

    [RelayCommand]
    private void Select(NavItem? item)
    {
        if (item is null) return;
        try
        {
            Selected = item;
            CurrentView = item.ViewModel;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel.Select] {ex}");
        }
    }
}

public record NavItem(string Title, object ViewModel, string Index);
