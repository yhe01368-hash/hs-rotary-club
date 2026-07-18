using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HsRotaryClub.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public ObservableCollection<NavItem> Modules { get; } = new();

    [ObservableProperty]
    private NavItem? _selected;

    [ObservableProperty]
    private object? _currentView;

    public MainWindowViewModel(
        HomeViewModel home,
        ClubManagementViewModel clubs,
        MemberViewModel member,
        ClubCollectionViewModel collection,
        FriendlyClubViewModel friendly,
        AttendanceViewModel attendance,
        OtherTransactionViewModel otherTxn,
        AccountingViewModel accounting,
        MailViewModel mail)
    {
        Modules.Add(new NavItem("🏠 首頁",     home,        "01"));
        Modules.Add(new NavItem("🏢 社團管理", clubs,       "02"));
        Modules.Add(new NavItem("👤 社員資料", member,     "03"));
        Modules.Add(new NavItem("💰 會內收款", collection, "04"));
        Modules.Add(new NavItem("?? ?社?款", friendly,   "05"));
                Modules.Add(new NavItem("?? 例?出?", attendance, "06"));
        Modules.Add(new NavItem("📒 其它收支", otherTxn,   "07"));
        Modules.Add(new NavItem("📊 會計月報", accounting, "08"));
        Modules.Add(new NavItem("📧 信件作業", mail,       "09"));
        Selected = Modules[0];
        CurrentView = Selected.ViewModel;
    }

    [RelayCommand]
    private void Select(NavItem? item)
    {
        if (item is null) return;
        Selected = item;
        CurrentView = item.ViewModel;
    }
}

public record NavItem(string Title, object ViewModel, string Index);
