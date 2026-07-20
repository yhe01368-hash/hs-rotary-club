using System.IO;
using System.Windows;
using HsRotaryClub.Infrastructure;

namespace HsRotaryClub.App.Controls;

public partial class LicenseAdminDialog : Window
{
    public LicenseAdminDialog()
    {
        InitializeComponent();
        LoadMachineId();
        LoadCurrentLicense();
    }

    private void LoadMachineId()
    {
        try
        {
            var mid = LicenseService.GetMachineId() ?? "(無)";
            MachineIdText.Text = mid;
        }
        catch (Exception ex)
        {
            MachineIdText.Text = $"(讀取失敗: {ex.Message})";
        }
    }

    private void LoadCurrentLicense()
    {
        var path = LicenseService.GetLicensePath();
        if (File.Exists(path))
        {
            try { LicenseContent.Text = File.ReadAllText(path); }
            catch (Exception ex) { LicenseContent.Text = $"讀取失敗: {ex.Message}"; }
        }
        else
        {
            LicenseContent.Text = "(license.dat 不存在 → Trial mode)";
        }
    }

    private void IssueBtn_Click(object sender, RoutedEventArgs e)
    {
        var issuedTo = (IssuedToBox.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(issuedTo))
        {
            MessageBox.Show(this, "請輸入『發行給』", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!int.TryParse(DaysBox.Text, out var days) || days < 0)
        {
            MessageBox.Show(this, "有效天數須 >= 0 (0 = 永久)", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!int.TryParse(MaxClubsBox.Text, out var maxClubs) || maxClubs < 0)
        {
            MessageBox.Show(this, "MaxClubs 須 >= 0 (0 = 不限)", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var info = new LicenseInfo
        {
            IssuedTo = issuedTo,
            Issuer = (IssuerBox.Text ?? "").Trim(),
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = days > 0 ? DateTime.UtcNow.AddDays(days) : null,
            MachineId = BindMachine.IsChecked == true ? (LicenseService.GetMachineId() ?? "") : "",
            MaxClubs = maxClubs,
        };
        LicenseService.Issue(info);
        LoadCurrentLicense();
        MessageBox.Show(this, $"License 已發行:\n  發行給: {info.IssuedTo}\n  到期: {info.ExpiresAt:yyyy-MM-dd 或 HH:mm 不存在(永久)}\n  綁機: {(string.IsNullOrEmpty(info.MachineId) ? "(不綁)" : info.MachineId)}",
            "完成", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Reload_Click(object sender, RoutedEventArgs e)
    {
        LoadCurrentLicense();
        LoadMachineId();
    }

    public static void Show(Window? owner = null)
    {
        var dlg = new LicenseAdminDialog();
        // v0.30: Owner 設前先檢查,Window 可能已被 close,避免 InvalidOperationException
            if (owner is not null)
            {
                try { dlg.Owner = owner; }
                catch (InvalidOperationException) { /* owner disposed */ }
            }
        dlg.ShowDialog();
    }
}