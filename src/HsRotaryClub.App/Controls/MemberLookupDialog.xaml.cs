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

    /// <summary>
    /// v0.48: 計算當月未繳會費的 member codes — 用 ClubCollections 判斷 (本月已有收款記錄 = 已繳).
    /// 之前用 ReceivableSpecs 但實際應收是 user 自己手設的,沒資料就沒人是 overdue.
    /// </summary>
    private HashSet<int> LoadOverdueMemberCodes()
    {
        var overdue = new HashSet<int>();
        try
        {
            var today = DateTime.Today;
            // 載入本月已繳費的 member codes (用 ClubCollections 收款記錄)
            var paidCodes = _db.ClubCollections.AsNoTracking()
                .Where(c => c.Year == today.Year && c.Month == today.Month
                         && c.MemberCode > 0)
                .Select(c => c.MemberCode)
                .Distinct()
                .ToList();
            var paid = new HashSet<int>(paidCodes);
            // 全現任 member 中扣掉已繳 = 未繳
            var allCurrentCodes = _db.Members.AsNoTracking()
                .Where(m => m.IsCurrent)
                .Select(m => m.Code)
                .ToList();
            foreach (var c in allCurrentCodes)
            {
                if (!paid.Contains(c)) overdue.Add(c);
            }
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
