using System.IO;
using System.Windows;
using HsRotaryClub.Infrastructure;
using Microsoft.Win32;

namespace HsRotaryClub.App.Controls;

public partial class MigrationDialog : Window
{
    private readonly RotaryDbContext _db;

    public MigrationDialog(RotaryDbContext db)
    {
        InitializeComponent();
        _db = db;
    }

    private void BrowseBtn_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Access mdb (*.mdb)|*.mdb|所有檔案 (*.*)|*.*",
            Title = "選擇舊版 mdb 檔 (例: TS81.mdb)",
            InitialDirectory = @"C:\Program Files (x86)\Project1",
        };
        if (dlg.ShowDialog(this) == true)
        {
            PathBox.Text = dlg.FileName;
        }
    }

    private void AnalyzeBtn_Click(object sender, RoutedEventArgs e)
    {
        var path = PathBox.Text;
        if (string.IsNullOrWhiteSpace(path))
        {
            MessageBox.Show(this, "請先選擇 mdb 檔", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!File.Exists(path))
        {
            TablesText.Text = $"❌ 找不到檔案: {path}";
            return;
        }
        TablesText.Text = $"✅ 找到檔案: {path}\nSize: {new FileInfo(path).Length:N0} bytes\n備註:實際分析會在「開始遷移」時執行 (需要安裝 Microsoft Access Database Engine)。";
    }

    private void MigrateBtn_Click(object sender, RoutedEventArgs e)
    {
        var path = PathBox.Text;
        if (string.IsNullOrWhiteSpace(path))
        {
            MessageBox.Show(this, "請先選擇 mdb 檔", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var dryRun = DryRun.IsChecked == true;
            var result = MigrationEngine.Migrate(path, _db, dryRun: dryRun);
            ResultText.Text = result.Summary;
            if (result.Errors.Count > 0)
            {
                ResultText.Text += "\n\n錯誤:\n" + string.Join("\n", result.Errors);
            }
            if (result.Warnings.Count > 0)
            {
                ResultText.Text += "\n\n警告:\n" + string.Join("\n", result.Warnings);
            }
        }
        catch (Exception ex)
        {
            ResultText.Text = $"❌ 遷移失敗: {ex.Message}";
        }
    }

    public static void Show(RotaryDbContext db, Window? owner = null)
    {
        var dlg = new MigrationDialog(db);
        if (owner is not null) dlg.Owner = owner;
        dlg.ShowDialog();
    }
}