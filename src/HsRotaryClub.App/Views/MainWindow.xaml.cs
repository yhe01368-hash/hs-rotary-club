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
        try
        {
            if (sender is Button b && b.Tag is NavItem item && DataContext is MainWindowViewModel vm)
            {
                vm.SelectCommand.Execute(item);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OnNavClick] {ex}");
            MessageBox.Show($"切換頁面失敗: {ex.Message}", "錯誤",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
