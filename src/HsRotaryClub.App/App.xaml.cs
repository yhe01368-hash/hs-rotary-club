using System.Windows;
using HsRotaryClub.App.Controls;
using HsRotaryClub.App.Infrastructure;
using HsRotaryClub.App.ViewModels;
using HsRotaryClub.App.Views;
using HsRotaryClub.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HsRotaryClub.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        services.AddSingleton<DbContextOptions<RotaryDbContext>>(sp =>
        {
            var dbPath = DbPaths.Get();
            var b = new DbContextOptionsBuilder<RotaryDbContext>();
            b.UseSqlite($"Data Source={dbPath}");
            return b.Options;
        });
        services.AddScoped<RotaryDbContext>();

        // Seed at startup (idempotent)
        services.AddSingleton<DbInitializer>();

        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MainWindow>(sp => new MainWindow(sp.GetRequiredService<MainWindowViewModel>()));

        services.AddTransient<HomeViewModel>();
        services.AddTransient<ClubManagementViewModel>();
        services.AddTransient<MemberViewModel>();
        services.AddTransient<ClubCollectionViewModel>();
        services.AddTransient<FriendlyClubViewModel>();
        services.AddTransient<AttendanceViewModel>();  // v0.11
        services.AddSingleton<CurrentClubContext>();  // v0.7 A5 — 全 app 共用「當前操作社」

        Services = services.BuildServiceProvider();

        // 確保 schema 建好 + seed
        using (var scope = Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<DbInitializer>().Initialize();
        }

        // v0.9 — license 載入 + 狀態檢查
        var license = LicenseService.LoadAndValidate();
        // TODO: Trial mode 限制 1 club / 7 天 (v0.9.1)
        System.Diagnostics.Debug.WriteLine($"[License] {LicenseService.Describe(license)}");

        // v0.7 A4 — 啟動時選社 (Db 裡有 2+ 個 active club 才拉 picker,只有 default 一個直接跳過)
        var currentClubCtx = Services.GetRequiredService<CurrentClubContext>();
        using (var initScope = Services.CreateScope())
        {
            var initDb = initScope.ServiceProvider.GetRequiredService<RotaryDbContext>();
            var clubCount = initDb.Clubs.Count(c => c.IsActive);
            if (clubCount >= 2)
            {
                var picked = ClubPickerDialog.Pick(initDb);
                if (picked is not null)
                {
                    currentClubCtx.SetCurrent(picked.Id, picked.Name);
                }
                // 取消就用 default (1)
            }
        }

        var main = Services.GetRequiredService<MainWindow>();
        main.Show();
    }
}
