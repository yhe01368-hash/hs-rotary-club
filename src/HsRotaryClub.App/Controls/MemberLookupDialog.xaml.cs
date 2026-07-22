using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using HsRotaryClub.App.Views;
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

    /// <summary>v0.44: 計算當月未繳會費 (ReceivableSpecs 中 OutstandingAmount > 0) 的 member codes.</summary>
    private HashSet<int> LoadOverdueMemberCodes()
    {
        var overdue = new HashSet<int>();
        try
        {
            var today = DateTime.Today;
            // 載入所有 ReceivableSpecs 同 Year/Month 且 OutstandingAmount > 0 的 member codes
            var q = _db.MonthlyReceivableSpecs.AsNoTracking()
                .Where(s => s.Year == today.Year && s.Month == today.Month && s.OutstandingAmount > 0)
                .Select(s => s.MemberCode);
            foreach (var c in q.Distinct()) overdue.Add(c);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoadOverdueMemberCodes] {ex}");
        }
        return overdue;
    }

    private void LoadAll(string? filter = null)
    {
        Results.Clear();
        var overdueCodes = LoadOverdueMemberCodes();
        var q = _db.Members.AsNoTracking().Where(m => m.IsCurrent);
        if (!string.IsNullOrWhiteSpace(filter))
        {
            q = q.Where(m =>
                m.Name.Contains(filter) ||
                (m.EnglishName != null && m.EnglishName.Contains(filter)) ||
                m.Code.ToString().Contains(filter));
        }
        foreach (var m in q.OrderBy(m => m.Code).ToList())
        {
            // v0.44: 用 IsOverdue 標記,給 XAML DataTrigger 用
            m.IsOverdue = overdueCodes.Contains(m.Code);
            Results.Add(m);
        }
        // v0.46: Member entity 不實作 INPC,所以 IsOverdue 改值後 UI 不自動 refresh.
        // 強制 ListBox 重畫 ItemTemplate (讓 DataTrigger 重跑).
        MembersList.Items.Refresh();
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

    public static Member? Ask(Window? owner = null)
    {
        var dlg = new MemberLookupDialog();
        if (owner is null)
        {
            owner = App.Services?.GetService(typeof(MainWindow)) as Window
                    ?? Application.Current?.MainWindow;
        }
        if (owner is not null)
        {
            try { dlg.Owner = owner; }
            catch (InvalidOperationException) { /* owner disposed */ }
        }
        return dlg.ShowDialog() == true ? dlg.SelectedMember : null;
    }
}
