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
        MemberViewModel member,
        ClubCollectionViewModel collection,
        FriendlyClubViewModel friendly)
    {
        Modules.Add(new NavItem("🏠 首頁",   home,        "01"));
        Modules.Add(new NavItem("👤 社員資料", member,     "02"));
        Modules.Add(new NavItem("💰 會內收款", collection, "03"));
        Modules.Add(new NavItem("🤝 友社捐款", friendly,   "04"));
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
