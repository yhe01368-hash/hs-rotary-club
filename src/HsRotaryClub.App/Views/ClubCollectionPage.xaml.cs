using System.Windows;
using System.Windows.Controls;
using HsRotaryClub.App.ViewModels;

namespace HsRotaryClub.App.Views;

public partial class ClubCollectionPage : UserControl
{
    public ClubCollectionPage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is ClubCollectionViewModel vm)
            {
                // v0.42: 進頁時不要自動選第一筆,讓 user 主動選.
                vm.Selected = null;
            }
        };
    }

    /// <summary>
    /// v0.42: 「取消選取」按鈕 handler. 清空 Selected 並讓 user 可從空白開始填.
    /// </summary>
    private void ClearSelection_Click(object sender, RoutedEventArgs e)
    {
        ClearSelection();
    }

    /// <summary>
    /// v0.42: 取消 ListBox 選取. 清空 Selected 並讓 user 可從空白開始填.
    /// </summary>
    public void ClearSelection()
    {
        if (DataContext is ClubCollectionViewModel vm)
        {
            vm.Selected = null;
        }
        CollectionsList.SelectedItem = null;
    }
}
