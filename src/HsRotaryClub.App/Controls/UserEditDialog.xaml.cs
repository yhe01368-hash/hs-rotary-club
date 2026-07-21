using System.Windows;
using HsRotaryClub.Domain;
using HsRotaryClub.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HsRotaryClub.App.Controls;

public partial class UserEditDialog : Window
{
    private readonly User? _existing;

    public UserEditDialog(User? existing)
    {
        InitializeComponent();
        _existing = existing;
        if (_existing is not null)
        {
            UsernameBox.Text = _existing.Username;
            UsernameBox.IsEnabled = false; // 帳號建立後不可改
            DisplayBox.Text = _existing.DisplayName;
            RoleBox.SelectedItem = _existing.Role.ToString();
            PasswordLabel.Text = "新密碼 (留空 = 不改)";
            Title = $"編輯使用者: {_existing.Username}";
        }
        else
        {
            RoleBox.SelectedIndex = 0; // Admin default
            Title = "新增使用者";
        }
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;
        var username = UsernameBox.Text.Trim();
        var display = DisplayBox.Text.Trim();
        var roleStr = RoleBox.SelectedItem as string ?? "Member";
        var password = PasswordBox.Password;

        if (string.IsNullOrEmpty(username))
        {
            ErrorText.Text = "帳號必填";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        if (_existing is null && string.IsNullOrEmpty(password))
        {
            ErrorText.Text = "新增使用者必須設定密碼";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        if (!Enum.TryParse<UserRole>(roleStr, out var role))
        {
            ErrorText.Text = "角色不合法";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            using var scope = App.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RotaryDbContext>();

            if (_existing is null)
            {
                // 新增
                if (db.Users.Any(u => u.Username == username))
                {
                    ErrorText.Text = $"帳號 {username} 已存在";
                    ErrorText.Visibility = Visibility.Visible;
                    return;
                }
                var u = new User
                {
                    Username = username,
                    DisplayName = display,
                    Role = role,
                    PasswordHash = PasswordHasher.Hash(password),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                };
                db.Users.Add(u);
                if (!db.TrySaveChanges(out var err))
                {
                    ErrorText.Text = $"儲存失敗: {err}";
                    ErrorText.Visibility = Visibility.Visible;
                    return;
                }
            }
            else
            {
                // 編輯
                var attached = db.Users.FirstOrDefault(u => u.Id == _existing.Id);
                if (attached is null)
                {
                    ErrorText.Text = "使用者不存在 (可能已被刪除)";
                    ErrorText.Visibility = Visibility.Visible;
                    return;
                }
                attached.DisplayName = display;
                attached.Role = role;
                if (!string.IsNullOrEmpty(password))
                {
                    attached.PasswordHash = PasswordHasher.Hash(password);
                }
                if (!db.TrySaveChanges(out var err))
                {
                    ErrorText.Text = $"儲存失敗: {err}";
                    ErrorText.Visibility = Visibility.Visible;
                    return;
                }
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ErrorText.Text = $"錯誤: {ex.Message}";
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
