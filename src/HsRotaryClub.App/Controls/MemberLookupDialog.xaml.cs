using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using HsRotaryClub.Domain;
using HsRotaryClub.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HsRotaryClub.App.Controls;

public partial class MemberLookupDialog : Window
{
    private readonly RotaryDbContext _db;
    public ObservableCollection<Member> Results { get; } = new();
    public Member? SelectedMember { get; private set; }

    public MemberLookupDialog()
    {
        InitializeComponent();
        _db = App.Services.CreateScope().ServiceProvider.GetRequiredService<RotaryDbContext>();
        MembersList.ItemsSource = Results;
        LoadAll();
    }

    private void LoadAll(string? filter = null)
    {
        Results.Clear();
        var q = _db.Members.AsNoTracking().Where(m => m.IsCurrent);
        if (!string.IsNullOrWhiteSpace(filter))
        {
            q = q.Where(m =>
                m.Name.Contains(filter) ||
                (m.EnglishName != null && m.EnglishName.Contains(filter)) ||
                m.Code.ToString().Contains(filter));
        }
        foreach (var m in q.OrderBy(m => m.Code).ToList())
            Results.Add(m);
        MembersList.ItemsSource = Results;
    }

    private void FilterBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        => LoadAll(FilterBox.Text);

    private void MembersList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        SelectedMember = MembersList.SelectedItem as Member;
        SelectedInfo.Text = SelectedMember is null
            ? "(尚未選擇)"
            : $"已選擇: {SelectedMember.Code} {SelectedMember.Name}";
        OkButton.IsEnabled = SelectedMember is not null;
    }

    private void MembersList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SelectedMember is not null) DialogResult = true;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedMember is not null) DialogResult = true;
    }

    /// <summary>Static helper:彈出 + 回社員(null=取消)。</summary>
    public static Member? Ask(Window? owner = null)
    {
        var dlg = new MemberLookupDialog();
        if (owner is not null) dlg.Owner = owner;
        return dlg.ShowDialog() == true ? dlg.SelectedMember : null;
    }
}
