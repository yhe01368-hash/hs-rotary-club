using System.Windows;
using System.Windows.Controls;
using HsRotaryClub.App.Infrastructure;
using HsRotaryClub.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HsRotaryClub.App.Views;

public partial class ClubManagementPage : UserControl
{
    private readonly CurrentClubContext _currentClubCtx;
    private readonly RotaryDbContext _db;

    public ClubManagementPage(RotaryDbContext db, CurrentClubContext currentClubCtx)
    {
        InitializeComponent();
        _db = db;
        _currentClubCtx = currentClubCtx;
        // 不設 DataContext,保留外部 MainWindow 傳進的 VM
    }

    private void OpenImportExport_Click(object sender, RoutedEventArgs e)
    {
        var owner = Window.GetWindow(this);
        HsRotaryClub.App.Controls.ImportExportDialog.Show(
            _db,
            _currentClubCtx.CurrentClubId,
            _currentClubCtx.CurrentClubName,
            owner);
    }

    private void OpenLicenseAdmin_Click(object sender, RoutedEventArgs e)
    {
        var owner = Window.GetWindow(this);
        HsRotaryClub.App.Controls.LicenseAdminDialog.Show(owner);
    }
}