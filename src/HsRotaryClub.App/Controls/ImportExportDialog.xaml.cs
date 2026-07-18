using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using HsRotaryClub.Infrastructure;
using Microsoft.Win32;

namespace HsRotaryClub.App.Controls;

public partial class ImportExportDialog : Window
{
    private readonly RotaryDbContext _db;
    private readonly int _currentClubId;
    private readonly string _currentClubName;

    public ImportExportDialog(RotaryDbContext db, int currentClubId, string currentClubName)
    {
        InitializeComponent();
        _db = db;
        _currentClubId = currentClubId;
        _currentClubName = currentClubName;
        CurrentClubLabel.Text = $"({currentClubId}) {currentClubName}";
        ShowExportPanel();
    }

    private void Tab_Click(object sender, RoutedEventArgs e)
    {
        if (sender == ExportTab)
        {
            ShowExportPanel();
        }
        else if (sender == ImportTab)
        {
            ShowImportPanel();
        }
    }

    private void ShowExportPanel()
    {
        ExportTab.IsChecked = true;
        ImportTab.IsChecked = false;
        ExportPreview.Visibility = Visibility.Visible;
        ImportPreview.Visibility = Visibility.Collapsed;
        PreviewBox.Visibility = Visibility.Collapsed;
        PreviewHeader.Visibility = Visibility.Collapsed;
        ResultMessage.Text = "";
        PathBox.Text = "";
    }

    private void ShowImportPanel()
    {
        ExportTab.IsChecked = false;
        ImportTab.IsChecked = true;
        ExportPreview.Visibility = Visibility.Collapsed;
        ImportPreview.Visibility = Visibility.Visible;
        ResultMessage.Text = "";
        PathBox.Text = "";
        PreviewBox.Visibility = Visibility.Collapsed;
        PreviewHeader.Visibility = Visibility.Collapsed;
    }

    private void BrowseBtn_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = ExportTab.IsChecked == true
                ? "JSON 匯出檔 (*.json)|*.json|所有檔案 (*.*)|*.*"
                : "JSON 匯入檔 (*.json)|*.json|所有檔案 (*.*)|*.*",
            Title = ExportTab.IsChecked == true ? "選擇匯出目的地" : "選擇要匯入的 JSON 檔",
        };
        if (ExportTab.IsChecked == true)
        {
            dlg.FileName = $"HsRotaryClub_export_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        }
        if (dlg.ShowDialog(this) == true)
        {
            PathBox.Text = dlg.FileName;
            if (ImportTab.IsChecked == true)
            {
                ShowPreview(dlg.FileName);
            }
        }
    }

    private void ShowPreview(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            PreviewBox.Visibility = Visibility.Visible;
            PreviewHeader.Visibility = Visibility.Visible;
            PreviewHeader.Text = "📊 預覽內容:";

            int Get(string name) => root.TryGetProperty(name, out var arr) && arr.ValueKind == JsonValueKind.Array ? arr.GetArrayLength() : 0;
            PreviewClubsCount.Text = $"🏢 社團: {Get("clubs")} 個";
            PreviewMembersCount.Text = $"👤 社員: {Get("members")} 人";
            PreviewCollectionsCount.Text = $"💰 會內收款: {Get("collections")} 筆";
            PreviewReceivablesCount.Text = $"📋 月度應收: {Get("receivables")} 筆";
            PreviewFriendlyCount.Text = $"🤝 友社: {Get("friendlyClubs")} 個";
            PreviewDonationsCount.Text = $"💸 友社捐款: {Get("donations")} 筆";

            if (root.TryGetProperty("schemaVersion", out var sv))
            {
                PreviewSchema.Text = $"Schema v{sv.GetInt32()}";
            }
            if (root.TryGetProperty("exportedAt", out var ea))
            {
                PreviewExportedAt.Text = $"匯出時間: {ea.GetDateTime():yyyy-MM-dd HH:mm:ss}";
            }
        }
        catch (Exception ex)
        {
            PreviewBox.Visibility = Visibility.Visible;
            PreviewHeader.Visibility = Visibility.Visible;
            PreviewHeader.Text = "❌ JSON 解析失敗";
            PreviewClubsCount.Text = ex.Message;
        }
    }

    private void ExecuteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (ExportTab.IsChecked == true)
        {
            DoExport();
        }
        else
        {
            DoImport();
        }
    }

    private void DoExport()
    {
        var path = PathBox.Text;
        if (string.IsNullOrWhiteSpace(path))
        {
            ResultMessage.Text = "❌ 請先選擇匯出目的地";
            ResultMessage.Foreground = System.Windows.Media.Brushes.DarkRed;
            return;
        }
        try
        {
            int? onlyClub = ExpOneClub.IsChecked == true ? _currentClubId : (int?)null;
            var json = DataTransferEngine.ExportToJson(_db, clubId: onlyClub);
            File.WriteAllText(path, json);
            ResultMessage.Text = $"✅ 匯出成功: {path}";
            ResultMessage.Foreground = System.Windows.Media.Brushes.DarkGreen;
        }
        catch (Exception ex)
        {
            ResultMessage.Text = $"❌ 匯出失敗: {ex.Message}";
            ResultMessage.Foreground = System.Windows.Media.Brushes.DarkRed;
        }
    }

    private void DoImport()
    {
        var path = PathBox.Text;
        if (string.IsNullOrWhiteSpace(path))
        {
            ResultMessage.Text = "❌ 請先選擇要匯入的 JSON 檔";
            ResultMessage.Foreground = System.Windows.Media.Brushes.DarkRed;
            return;
        }
        try
        {
            var json = File.ReadAllText(path);
            var skip = SkipExisting.IsChecked == true;
            var result = DataTransferEngine.ImportFromJson(_db, json, skipExisting: skip);
            ResultMessage.Text = $"✅ {result.Summary}";
            ResultMessage.Foreground = System.Windows.Media.Brushes.DarkGreen;
            // re-preview 顯示現在 db 內容
            ShowPreview(path);
        }
        catch (Exception ex)
        {
            ResultMessage.Text = $"❌ 匯入失敗: {ex.Message}";
            ResultMessage.Foreground = System.Windows.Media.Brushes.DarkRed;
        }
    }

    /// <summary>Static helper:彈出 import/export dialog。</summary>
    public static void Show(RotaryDbContext db, int currentClubId, string currentClubName, Window? owner = null)
    {
        var dlg = new ImportExportDialog(db, currentClubId, currentClubName);
        if (owner is not null) dlg.Owner = owner;
        dlg.ShowDialog();
    }
}
