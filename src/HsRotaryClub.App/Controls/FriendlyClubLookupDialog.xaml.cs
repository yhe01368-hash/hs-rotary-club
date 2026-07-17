using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using HsRotaryClub.App.Views;
using HsRotaryClub.Domain;
using HsRotaryClub.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HsRotaryClub.App.Controls;

public partial class FriendlyClubLookupDialog : Window
{
    private readonly RotaryDbContext _db;
    public ObservableCollection<FriendlyClub> Results { get; } = new();
    public FriendlyClub? SelectedClub { get; private set; }

    public FriendlyClubLookupDialog()
    {
        InitializeComponent();
        _db = App.Services.CreateScope().ServiceProvider.GetRequiredService<RotaryDbContext>();
        LoadAll();
    }

    private void LoadAll(string? filter = null)
    {
        Results.Clear();
        var q = _db.FriendlyClubs.AsNoTracking().Where(c => c.IsActive);
        if (!string.IsNullOrWhiteSpace(filter))
        {
            q = q.Where(c =>
                c.ClubCode.Contains(filter) ||
                c.ClubName.Contains(filter));
        }
        foreach (var c in q.OrderBy(c => c.ClubCode).ToList())
            Results.Add(c);
        List.ItemsSource = Results;
    }

    private void FilterBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        => LoadAll(FilterBox.Text);

    private void List_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        SelectedClub = List.SelectedItem as FriendlyClub;
        SelectedInfo.Text = SelectedClub is null
            ? "(尚未選擇)"
            : $"已選擇: {SelectedClub.ClubCode} {SelectedClub.ClubName}";
        OkButton.IsEnabled = SelectedClub is not null;
    }

    private void List_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SelectedClub is not null) DialogResult = true;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedClub is not null) DialogResult = true;
    }

    public static FriendlyClub? Ask(Window? owner = null)
    {
        var dlg = new FriendlyClubLookupDialog();
        if (owner is null)
        {
            owner = App.Services?.GetService(typeof(MainWindow)) as Window
                    ?? Application.Current?.MainWindow;
        }
        if (owner is not null) dlg.Owner = owner;
        return dlg.ShowDialog() == true ? dlg.SelectedClub : null;
    }
}
