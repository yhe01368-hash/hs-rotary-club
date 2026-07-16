using System.Windows;
using System.Windows.Controls;
using HsRotaryClub.App.ViewModels;

namespace HsRotaryClub.App.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void OnNavClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is NavItem item && DataContext is MainWindowViewModel vm)
        {
            vm.SelectCommand.Execute(item);
        }
    }
}
