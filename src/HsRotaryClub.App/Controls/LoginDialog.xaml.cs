using System.Windows;
using System.Windows.Controls;
using HsRotaryClub.App.Infrastructure;
using HsRotaryClub.Domain;
using HsRotaryClub.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HsRotaryClub.App.Controls;

public partial class LoginDialog : UserControl
{
    public User? AuthenticatedUser { get; private set; }

    public LoginDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => UsernameBox.Focus();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;
        var username = UsernameBox.Text.Trim();
        var password = PasswordBox.Password;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ErrorText.Text = "請輸入帳號與密碼";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RotaryDbContext>();
            var user = db.Users.AsNoTracking().FirstOrDefault(u => u.Username == username && u.IsActive);
            if (user is null || !PasswordHasher.Verify(password, user.PasswordHash))
            {
                ErrorText.Text = "帳號或密碼錯誤";
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            user.LastLoginAt = DateTime.UtcNow;
            db.Users.Update(user);
            db.SaveChanges();

            AuthenticatedUser = user;

            // 把 current user context 設好 (singleton)
            var ctx = App.Services.GetRequiredService<CurrentUserContext>();
            ctx.SetCurrent(user);

            var win = System.Windows.Window.GetWindow(this);
            if (win is not null)
            {
                win.DialogResult = true;
                win.Close();
            }
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"登入錯誤: {ex.Message}";
            ErrorText.Visibility = Visibility.Visible;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        var win = System.Windows.Window.GetWindow(this);
        if (win is not null)
        {
            win.DialogResult = false;
            win.Close();
        }
    }

    /// <summary>
    /// v0.38: 顯示登入 dialog,return AuthenticatedUser (null = 使用者取消).
    /// 失敗 / 取消時 App.OnStartup 會中止啟動.
    /// </summary>
    public static User? Show(Window? owner = null)
    {
        var dlgHost = new Window
        {
            Title = "HsRotaryClub 登入",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            WindowStyle = WindowStyle.SingleBorderWindow,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
        };
        var control = new LoginDialog();
        dlgHost.Content = control;
        if (owner is not null)
        {
            try { dlgHost.Owner = owner; } catch (InvalidOperationException) { /* owner disposed */ }
        }
        var result = dlgHost.ShowDialog();
        return result == true ? control.AuthenticatedUser : null;
    }
}
