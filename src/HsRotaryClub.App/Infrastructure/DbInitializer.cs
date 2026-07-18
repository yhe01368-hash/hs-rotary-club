using System.IO;
using HsRotaryClub.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace HsRotaryClub.App.Infrastructure;

/// <summary>
/// App startup 跑一次:EnsureCreated + SeedIfEmpty。
/// 之後改成 migrate-only (先生產了 dev.db 然後跑 migrations)。
/// v0.16.1: 偵測舊 db (無 Clubs table) → 砍掉重建,避免升級時 seed crash。
/// v0.16.1: try/catch 包 SeedIfEmpty → MessageBox 顯示詳細錯誤(不再 crash)。
/// </summary>
public sealed class DbInitializer
{
    private readonly DbContextOptions<RotaryDbContext> _options;

    public DbInitializer(DbContextOptions<RotaryDbContext> options)
    {
        _options = options;
    }

    public void Initialize()
    {
        var dbPath = DbPaths.Get();
        EnsureSchemaOrRecreate(dbPath);

        try
        {
            using var db = new RotaryDbContext(_options);
            SeedData.SeedIfEmpty(db);
        }
        catch (Exception ex)
        {
            // 不再讓 seed exception crash 整個 App — 顯示給 user + 寫 log
            var msg = $"DB seed 失敗: {ex.Message}\n\n詳細:\n{ex}";
            System.Diagnostics.Debug.WriteLine($"[DbInitializer] {msg}");
            System.Windows.MessageBox.Show(msg, "資料庫初始化錯誤",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 偵測 db 存在但無 Clubs table (升級舊 db schema 不 match 時) → 砍掉重建。
    /// 否則走 EF EnsureCreated (對完全空的 db 建表)。
    /// </summary>
    private void EnsureSchemaOrRecreate(string dbPath)
    {
        // 不存在 → 直接 EnsureCreated 建表
        if (!File.Exists(dbPath))
        {
            using var db = new RotaryDbContext(_options);
            db.Database.EnsureCreated();
            return;
        }

        // 存在但無 Clubs table → 砍掉重建
        if (!HasTable(dbPath, "Clubs"))
        {
            System.Diagnostics.Debug.WriteLine($"[DbInitializer] 舊 db 無 Clubs table,砍掉重建: {dbPath}");
            try { File.Delete(dbPath); } catch { /* 可能被 lock,繼續 — EnsureCreated 會 fail */ }
            try
            {
                // EF Core 自動用 connection string 的 db
                using var db = new RotaryDbContext(_options);
                db.Database.EnsureCreated();
            }
            catch
            {
                // 如果砍不掉,user 看到 seed error,引導手動砍
                throw;
            }
            return;
        }

        // 存在且 schema OK → 跳過 EnsureCreated
    }

    /// <summary>用 raw sqlite 查 db 有沒有指定 table。</summary>
    private static bool HasTable(string dbPath, string tableName)
    {
        try
        {
            var connStr = $"Data Source={dbPath}";
            using var conn = new SqliteConnection(connStr);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name";
            var p = cmd.CreateParameter();
            p.ParameterName = "$name";
            p.Value = tableName;
            cmd.Parameters.Add(p);
            var count = Convert.ToInt32(cmd.ExecuteScalar());
            return count > 0;
        }
        catch
        {
            // db 壞掉 / lock → 當作無 table
            return false;
        }
    }
}