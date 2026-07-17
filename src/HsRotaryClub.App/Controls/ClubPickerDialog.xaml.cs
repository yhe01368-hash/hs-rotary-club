using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using HsRotaryClub.Domain;
using HsRotaryClub.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HsRotaryClub.App.Controls;

public partial class ClubPickerDialog : Window
{
    private readonly RotaryDbContext _db;
    public ObservableCollection<Club> Results { get; } = new();
    public Club? SelectedClub { get; private set; }

    /// <summary>v0.7 A4 — 回傳使用者選的 club (或新建的),null = 取消。</summary>
    public static Club? Pick(RotaryDbContext db, Window? owner = null)
    {
        var dlg = new ClubPickerDialog(db);
        if (owner is not null) dlg.Owner = owner;
        return dlg.ShowDialog() == true ? dlg.SelectedClub : null;
    }

    private ClubPickerDialog(RotaryDbContext db)
    {
        InitializeComponent();
        _db = db;
        LoadAll();
        // 預設選 default
        var defaultClub = Results.FirstOrDefault(c => c.Id == HsRotaryClub.Domain.ClubDefaults.DefaultClubId);
        if (defaultClub is not null)
        {
            ClubsList.SelectedItem = defaultClub;
        }
    }

    private void LoadAll(string? filter = null)
    {
        Results.Clear();
        var q = _db.Clubs.AsNoTracking().Where(c => c.IsActive);
        if (!string.IsNullOrWhiteSpace(filter))
        {
            q = q.Where(c => c.Name.Contains(filter) || (c.District ?? "").Contains(filter));
        }
        foreach (var c in q.OrderBy(c => c.Name).ToList())
            Results.Add(c);
        ClubsList.ItemsSource = Results;
    }

    private void FilterBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        => LoadAll(FilterBox.Text);

    private void ClubsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        SelectedClub = ClubsList.SelectedItem as Club;
        OkButton.IsEnabled = SelectedClub is not null;
    }

    private void ClubsList_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SelectedClub is not null) { DialogResult = true; }
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedClub is not null) DialogResult = true;
    }

    private void NewClub_Click(object sender, RoutedEventArgs e)
    {
        var name = (NewNameBox.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show(this, "請輸入社名", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var c = new Club
        {
            Name = name,
            District = "",
            IsActive = true,
        };
        _db.Clubs.Add(c);
        if (!_db.TrySaveChanges(out var error))
        {
            MessageBox.Show(this, $"新增失敗: {error}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        NewNameBox.Clear();
        LoadAll(FilterBox.Text);
        ClubsList.SelectedItem = Results.FirstOrDefault(x => x.Id == c.Id);
        SelectedClub = c;
    }
}
