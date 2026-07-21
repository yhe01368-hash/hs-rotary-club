using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
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

    private static readonly string CrashLogPath = Path.Combine(
        Path.GetDirectoryName(typeof(App).Assembly.Location) ?? @"C:\Program Files (x86)\HsRotaryClub",
        "crash.log");

    public App()
    {
        // 1) 所有 thread 漏網的 .NET exception
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            LogFatal("AppDomain.UnhandledException", e.ExceptionObject as Exception);

        // 2) WPF UI thread 漏網
        DispatcherUnhandledException += (s, e) =>
        {
            LogFatal("Dispatcher.UnhandledException", e.Exception);
            // 不讓 WPF 整個死掉,給 user 看 message
            try
            {
                MessageBox.Show(
                    $"發生未處理錯誤:\n\n{e.Exception.Message}\n\n" +
                    $"詳情寫到 {CrashLogPath}",
                    "HsRotaryClub 嚴重錯誤",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch { /* messagebox 本身壞,放棄 */ }
            // 因為是 DispatcherException,要標記 handled 避免 app 死掉
            e.Handled = true;
        };
    }

    private void LogFatal(string source, Exception? ex)
    {
        try
        {
            var sw = new StringBuilder();
            sw.AppendLine($"=== {DateTime.Now:yyyy-MM-dd HH:mm:ss} [{source}] ===");
            for (int i = 0; i < 5 && ex != null; i++)
            {
                sw.AppendLine(ex.GetType().FullName + ": " + ex.Message);
                sw.AppendLine(ex.StackTrace);
                sw.AppendLine("---");
                ex = ex.InnerException;
            }
            File.AppendAllText(CrashLogPath, sw.ToString());
        }
        catch
        {
            // 連 log 都寫不了,放棄
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        try
        {
            // Build DI
            services.AddSingleton<DbContextOptions<RotaryDbContext>>(sp =>
            {
                var dbPath = DbPaths.Get();
                return new DbContextOptionsBuilder<RotaryDbContext>()
                    .UseSqlite($"Data Source={dbPath};Pooling=False")
                    .Options;
            });
            services.AddScoped<RotaryDbContext>();
            services.AddSingleton<CurrentClubContext>();
            services.AddSingleton<CurrentUserContext>();  // v0.38
            services.AddSingleton<DbInitializer>();
            services.AddTransient<HomeViewModel>();
            services.AddTransient<ClubManagementViewModel>();
            services.AddTransient<MemberViewModel>();
            services.AddTransient<ClubCollectionViewModel>();
            services.AddTransient<FriendlyClubViewModel>();
            services.AddTransient<AttendanceViewModel>();
            services.AddTransient<OtherTransactionViewModel>();
            services.AddTransient<AccountingViewModel>();
            services.AddTransient<MailViewModel>();
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<MainWindow>(sp =>
                new MainWindow(sp.GetRequiredService<MainWindowViewModel>()));

            Services = services.BuildServiceProvider();

            using (var scope = Services.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<DbInitializer>().Initialize();
            }

            // v0.9 license check (盡量吞,失敗也跑)
            try
            {
                var license = LicenseService.LoadAndValidate();
                System.Diagnostics.Debug.WriteLine($"[License] {license.Status}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[License] init failed: {ex.Message}");
            }

            var vm = Services.GetRequiredService<MainWindowViewModel>();
            var window = Services.GetRequiredService<MainWindow>();

            // v0.38: 顯示登入 dialog. 取消或失敗則 Shutdown,登入成功才顯示主視窗.
            var user = HsRotaryClub.App.Controls.LoginDialog.Show();
            if (user is null)
            {
                Shutdown(0);
                return;
            }
            System.Diagnostics.Debug.WriteLine($"[v0.38] login as {user.Username} ({user.Role})");

            window.Show();
        }
        catch (Exception ex)
        {
            LogFatal("OnStartup", ex);
            MessageBox.Show(
                $"App 啟動失敗:\n\n{ex.Message}\n\n" +
                $"詳情寫到 {CrashLogPath}",
                "HsRotaryClub 啟動失敗",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}