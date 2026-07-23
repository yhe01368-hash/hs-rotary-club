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
            Filter = "Access mdb (*.mdb)|*.mdb|All files (*.*)|*.*",
            Title = "選擇匯入的 mdb 檔 (例: TS81.mdb)",
            InitialDirectory = @"C:\Program Files (x86)\Project1",
        };
        if (dlg.ShowDialog(this) == true)
        {
            PathBox.Text = dlg.FileName;
        }
    }

    /// <summary>v0.59: 分析 mdb 內容列出 4 個 table</summary>
    private void AnalyzeBtn_Click(object sender, RoutedEventArgs e)
    {
        var path = PathBox.Text;
        if (string.IsNullOrWhiteSpace(path))
        {
            MessageBox.Show(this, "請先選 mdb 檔", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!File.Exists(path))
        {
            TablesText.Text = $"❌ 找不到檔案: {path}";
            return;
        }
        try
        {
            // Open connection and list tables
            var connStr = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={path};Persist Security Info=False;";
            using var conn = new System.Data.OleDb.OleDbConnection(connStr);
            conn.Open();
            var tables = MigrationEngine.GetTableNames(conn);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"✅ 找到 mdb: {Path.GetFileName(path)}");
            sb.AppendLine($"   Size: {new FileInfo(path).Length:N0} bytes");
            sb.AppendLine();
            sb.AppendLine($"📊 Tables ({tables.Count}):");
            // Mark known tables
            var known = new[] { "TS81", "MAT1", "MAT11", "MAT11_1" };
            foreach (var t in tables)
            {
                var mark = Array.IndexOf(known, t) >= 0 ? "✓" : "?";
                sb.AppendLine($"   {mark} {t}");
            }
            sb.AppendLine();
            sb.AppendLine("預期對應:");
            sb.AppendLine("  ✓ TS81    → 社員基本資料 (Member)");
            sb.AppendLine("  ✓ MAT1    → 年度組別 (AttendanceGroup)");
            sb.AppendLine("  ✓ MAT11   → 每月例會彙總 (不匯入,僅供參考)");
            sb.AppendLine("  ✓ MAT11_1 → 例會出席明細 (AttendanceRecord)");
            TablesText.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            // Try Jet fallback
            try
            {
                var connStr = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={path};Persist Security Info=False;";
                using var conn = new System.Data.OleDb.OleDbConnection(connStr);
                conn.Open();
                var tables = MigrationEngine.GetTableNames(conn);
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"✅ 找到 mdb: {Path.GetFileName(path)}");
                sb.AppendLine($"   Size: {new FileInfo(path).Length:N0} bytes");
                sb.AppendLine();
                sb.AppendLine($"📊 Tables ({tables.Count}):");
                foreach (var t in tables) sb.AppendLine($"   - {t}");
                TablesText.Text = sb.ToString();
            }
            catch (Exception ex2)
            {
                TablesText.Text = $"❌ 無法分析: {ex.Message}\n(需裝 Microsoft Access Database Engine 32-bit)\n{ex2.Message}";
            }
        }
    }

    private void MigrateBtn_Click(object sender, RoutedEventArgs e)
    {
        var path = PathBox.Text;
        if (string.IsNullOrWhiteSpace(path))
        {
            MessageBox.Show(this, "請先選 mdb 檔", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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
        // v0.30: Owner 設定前先檢查,Window 可能已被 close,避免 InvalidOperationException
        if (owner is not null)
        {
            try { dlg.Owner = owner; }
            catch (InvalidOperationException) { /* owner disposed */ }
        }
        dlg.ShowDialog();
    }
}
