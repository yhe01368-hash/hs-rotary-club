using System.Windows;
using HsRotaryClub.Domain;
using HsRotaryClub.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HsRotaryClub.App.Controls;

public partial class UserManagementDialog : Window
{
    private readonly List<User> _users = new();
    private readonly Microsoft.Extensions.DependencyInjection.IServiceScope _scope;

    public UserManagementDialog(Microsoft.Extensions.DependencyInjection.IServiceScope scope)
    {
        InitializeComponent();
        _scope = scope;
        LoadUsers();
    }

    private void LoadUsers()
    {
        try
        {
            var db = _scope.ServiceProvider.GetRequiredService<RotaryDbContext>();
            _users.Clear();
            _users.AddRange(db.Users.AsNoTracking().OrderBy(u => u.Id).ToList());
            UsersList.ItemsSource = null;
            UsersList.ItemsSource = _users;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"載入使用者失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private User? Selected() => UsersList.SelectedItem as User;

    private void AddUser_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new UserEditDialog(null) { Owner = this };
        if (dlg.ShowDialog() == true) LoadUsers();
    }

    private void ChangePassword_Click(object sender, RoutedEventArgs e)
    {
        var u = Selected();
        if (u is null) { MessageBox.Show("請先選一個使用者"); return; }
        var dlg = new UserEditDialog(u) { Owner = this };
        if (dlg.ShowDialog() == true) LoadUsers();
    }

    private void ToggleActive_Click(object sender, RoutedEventArgs e)
    {
        var u = Selected();
        if (u is null) { MessageBox.Show("請先選一個使用者"); return; }
        try
        {
            var db = _scope.ServiceProvider.GetRequiredService<RotaryDbContext>();
            var attached = db.Users.FirstOrDefault(x => x.Id == u.Id);
            if (attached is null) { MessageBox.Show("DB 找不到此使用者"); return; }
            attached.IsActive = !attached.IsActive;
            if (!db.TrySaveChanges(out var err))
            {
                MessageBox.Show($"切換失敗: {err}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            LoadUsers();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeleteUser_Click(object sender, RoutedEventArgs e)
    {
        var u = Selected();
        if (u is null) { MessageBox.Show("請先選一個使用者"); return; }
        if (u.Username == "admin")
        {
            MessageBox.Show("admin 帳號不可刪除(預設管理員)", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (MessageBox.Show($"確定要刪除 {u.Username} ({u.DisplayName})?",
                "確認刪除", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;
        try
        {
            var db = _scope.ServiceProvider.GetRequiredService<RotaryDbContext>();
            var attached = db.Users.FirstOrDefault(x => x.Id == u.Id);
            if (attached is null) { MessageBox.Show("DB 找不到此使用者"); return; }
            db.Users.Remove(attached);
            if (!db.TrySaveChanges(out var err))
            {
                MessageBox.Show($"刪除失敗: {err}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            LoadUsers();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    public static void Show(Window? owner = null)
    {
        using var scope = App.Services.CreateScope();
        var dlg = new UserManagementDialog(scope);
        if (owner is not null)
        {
            try { dlg.Owner = owner; } catch (InvalidOperationException) { }
        }
        dlg.ShowDialog();
    }
}
