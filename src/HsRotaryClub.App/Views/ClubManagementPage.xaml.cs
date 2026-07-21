using System.Windows;
using System.Windows.Controls;
using HsRotaryClub.App.Infrastructure;
using HsRotaryClub.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HsRotaryClub.App.Views;

public partial class ClubManagementPage : UserControl
{
    // WPF DataTemplate 預設 new XxxPage() — 無參 ctor. 所以 DI 服務從 App.Services 拿.
    private CurrentClubContext CurrentClubCtx =>
        App.Services.GetRequiredService<CurrentClubContext>();

    public ClubManagementPage()
    {
        InitializeComponent();
        // DataContext 由外部 (MainWindow 透過 DataTemplate) 帶入,不設 DataContext.
    }

    private void OpenImportExport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var owner = Window.GetWindow(this);
            // ImportExportDialog.Show 從 DI 拿 db 即可,不需要這裡注入.
            var db = App.Services.GetRequiredService<RotaryDbContext>();
            HsRotaryClub.App.Controls.ImportExportDialog.Show(
                db,
                CurrentClubCtx.CurrentClubId,
                CurrentClubCtx.CurrentClubName,
                owner);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OpenImportExport] {ex}");
            MessageBox.Show($"匯出/匯入失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenLicenseAdmin_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var owner = Window.GetWindow(this);
            HsRotaryClub.App.Controls.LicenseAdminDialog.Show(owner);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OpenLicenseAdmin] {ex}");
            MessageBox.Show($"License 管理失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// v0.39: 開啟使用者管理 dialog. admin 帳號自動有權限,其他 user 也可進入 (v0.39 不限制).
    /// </summary>
    private void OpenUserMgmt_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var owner = Window.GetWindow(this);
            HsRotaryClub.App.Controls.UserManagementDialog.Show(owner);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[OpenUserMgmt] {ex}");
            MessageBox.Show($"使用者管理失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
