using System.Windows;
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
        services.AddSingleton<CurrentClubContext>();  // v0.7 A5 — 全 app 共用「當前操作社」

        Services = services.BuildServiceProvider();

        // 蝣箔? schema 撱箇? + seed
        using (var scope = Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<DbInitializer>().Initialize();
        }

        var main = Services.GetRequiredService<MainWindow>();
        main.Show();
    }
}
