using System.Windows.Controls;

namespace HsRotaryClub.App.Views;

public partial class ClubCollectionPage : UserControl
{
    public ClubCollectionPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Grid.AutoGeneratingColumn += OnAutoGen;
    }

    private void OnAutoGen(object? sender, System.Windows.Controls.DataGridAutoGeneratingColumnEventArgs e)
    {
        // v0.1: 用 AutoGenerateColumns 自動展開, 之後改成顯式 Columns
        if (e.PropertyName is nameof(HsRotaryClub.Domain.ClubCollection.Id))
        {
            e.Column.Width = 50;
        }
    }
}
