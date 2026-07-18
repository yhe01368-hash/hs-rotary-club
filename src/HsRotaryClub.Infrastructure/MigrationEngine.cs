using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Runtime.Versioning;
using HsRotaryClub.Domain;
using Microsoft.EntityFrameworkCore;

namespace HsRotaryClub.Infrastructure;

/// <summary>
/// v0.15 — 從舊版 VB6 + Jet .mdb 遷移到新 SQLite (ClubId = 1)。
///
/// 用法:
///   var result = MigrationEngine.Migrate("C:\\Program Files (x86)\\Project1\\TS81.mdb", ctx, dryRun: false);
///   → result.MembersImported / result.AttendanceGroupsImported / result.Errors
///
/// 設計:
/// - 用 System.Data.OleDb (需 ACE provider 已安裝)
/// - 預期 schema (舊版 TS81.mdb):
///     Table "Member" (Code, Name, EnglishName, Birthday, IdNumber, Mobile, Email, IsCurrent)
///     Table "TS81_MAT1" (Year, GroupName, GroupLeader, GroupMember, ShouldAttend, ActualAttend, MakeupAttend)
/// - 不認識的 table → skip,加 warning 到 result.Warnings
/// </summary>
public static class MigrationEngine
{
    /// <summary>需要 Microsoft Access Database Engine (ACE.OLEDB) — 32-bit</summary>
    [SupportedOSPlatform("windows")]
    public static MigrationResult Migrate(string mdbPath, RotaryDbContext target, int targetClubId = 1, bool dryRun = false)
    {
        var result = new MigrationResult { SourcePath = mdbPath, TargetClubId = targetClubId, DryRun = dryRun };

        if (!File.Exists(mdbPath))
        {
            result.Errors.Add($"找不到 mdb 檔: {mdbPath}");
            return result;
        }

        var connStr = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={mdbPath};Persist Security Info=False;";
        var connStrFallback = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={mdbPath};Persist Security Info=False;";

        OleDbConnection? conn = null;
        try
        {
            conn = new OleDbConnection(connStr);
            conn.Open();
        }
        catch
        {
            try
            {
                conn = new OleDbConnection(connStrFallback);
                conn.Open();
            }
            catch (Exception ex)
            {
                result.Errors.Add($"無法開啟 mdb (需要 ACE 或 Jet provider): {ex.Message}");
                result.Errors.Add("提示:裝 Microsoft Access Database Engine (32-bit) 或改用手動 CSV 匯入");
                return result;
            }
        }

        try
        {
            var tables = GetTableNames(conn);
            result.SourceTablesFound = tables;

            if (tables.Contains("Member"))
            {
                try
                {
                    var members = ReadMembers(conn, targetClubId);
                    result.MembersRead = members.Count;
                    if (!dryRun)
                    {
                        int added = 0, skipped = 0;
                        foreach (var m in members)
                        {
                            var exists = target.Members.Any(x => x.ClubId == m.ClubId && x.Code == m.Code);
                            if (exists) { skipped++; continue; }
                            target.Members.Add(m);
                            added++;
                        }
                        target.SaveChanges();
                        result.MembersImported = added;
                        result.MembersSkipped = skipped;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"讀取社員表失敗: {ex.Message}");
                }
            }
            else
            {
                result.Warnings.Add("找不到 'Member' table,略過社員遷移");
            }

            if (tables.Contains("TS81_MAT1"))
            {
                try
                {
                    var groups = ReadAttendanceGroups(conn, targetClubId);
                    result.AttendanceGroupsRead = groups.Count;
                    if (!dryRun)
                    {
                        int added = 0, skipped = 0;
                        foreach (var g in groups)
                        {
                            var exists = target.AttendanceGroups.Any(x => x.ClubId == g.ClubId && x.Year == g.Year && x.GroupName == g.GroupName);
                            if (exists) { skipped++; continue; }
                            target.AttendanceGroups.Add(g);
                            added++;
                        }
                        target.SaveChanges();
                        result.AttendanceGroupsImported = added;
                        result.AttendanceGroupsSkipped = skipped;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"讀取年度組別表失敗: {ex.Message}");
                }
            }
            else
            {
                result.Warnings.Add("找不到 'TS81_MAT1' table,略過年度組別遷移");
            }
        }
        finally
        {
            conn?.Dispose();
        }

        return result;
    }

    /// <summary>列出 mdb 內所有 user table name。</summary>
    [SupportedOSPlatform("windows")]
    public static List<string> GetTableNames(OleDbConnection conn)
    {
        var tables = new List<string>();
        var dt = conn.GetSchema("Tables");
        foreach (DataRow row in dt.Rows)
        {
            if (row["TABLE_TYPE"].ToString() == "TABLE")
            {
                tables.Add(row["TABLE_NAME"].ToString() ?? "");
            }
        }
        return tables;
    }

    /// <summary>讀社員主檔。欄位名寬鬆對應 (case-insensitive)。</summary>
    [SupportedOSPlatform("windows")]
    public static List<Member> ReadMembers(OleDbConnection conn, int clubId)
    {
        var result = new List<Member>();
        using var cmd = new OleDbCommand("SELECT * FROM [Member]", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            try
            {
                var m = new Member
                {
                    ClubId = clubId,
                    Code = TryGetInt(reader, "Code") ?? 0,
                    Name = TryGetString(reader, "Name") ?? "(無姓名)",
                    EnglishName = TryGetString(reader, "EnglishName"),
                    Birthday = TryGetDateOnly(reader, "Birthday"),
                    IdNumber = TryGetString(reader, "IdNumber"),
                    Mobile = TryGetString(reader, "Mobile"),
                    Email = TryGetString(reader, "Email"),
                    IsCurrent = TryGetBool(reader, "IsCurrent", true),
                };
                result.Add(m);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MigrationEngine] 跳過社員 row: {ex.Message}");
            }
        }
        return result;
    }

    /// <summary>讀年度組別。</summary>
    [SupportedOSPlatform("windows")]
    public static List<AttendanceGroup> ReadAttendanceGroups(OleDbConnection conn, int clubId)
    {
        var result = new List<AttendanceGroup>();
        using var cmd = new OleDbCommand("SELECT * FROM [TS81_MAT1]", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            try
            {
                var g = new AttendanceGroup
                {
                    ClubId = clubId,
                    Year = TryGetInt(reader, "Year", DateTime.Today.Year),
                    GroupName = TryGetString(reader, "GroupName") ?? "未分組",
                    GroupLeaderCode = TryGetInt(reader, "GroupLeader") ?? 0,
                    GroupMemberCode = TryGetInt(reader, "GroupMember") ?? 0,
                    ShouldAttend = TryGetInt(reader, "ShouldAttend") ?? 0,
                    ActualAttend = TryGetInt(reader, "ActualAttend") ?? 0,
                    MakeupAttend = TryGetInt(reader, "MakeupAttend") ?? 0,
                    IsActive = TryGetBool(reader, "IsActive", true),
                };
                result.Add(g);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MigrationEngine] 跳過組別 row: {ex.Message}");
            }
        }
        return result;
    }

    // helper: 寬鬆讀欄位
    private static int? TryGetInt(DbDataReader r, string col)
    {
        try
        {
            var ord = r.GetOrdinal(col);
            if (r.IsDBNull(ord)) return null;
            return Convert.ToInt32(r.GetValue(ord));
        }
        catch { return null; }
    }
    private static int TryGetInt(DbDataReader r, string col, int defaultValue)
        => TryGetInt(r, col) ?? defaultValue;
    private static string? TryGetString(DbDataReader r, string col)
    {
        try
        {
            var ord = r.GetOrdinal(col);
            if (r.IsDBNull(ord)) return null;
            return r.GetString(ord);
        }
        catch { return null; }
    }
    private static bool TryGetBool(DbDataReader r, string col, bool defaultValue)
    {
        try
        {
            var ord = r.GetOrdinal(col);
            if (r.IsDBNull(ord)) return defaultValue;
            return Convert.ToBoolean(r.GetValue(ord));
        }
        catch { return defaultValue; }
    }
    private static DateOnly? TryGetDateOnly(DbDataReader r, string col)
    {
        try
        {
            var ord = r.GetOrdinal(col);
            if (r.IsDBNull(ord)) return null;
            var v = r.GetValue(ord);
            if (v is DateTime dt) return DateOnly.FromDateTime(dt);
            return null;
        }
        catch { return null; }
    }
}

public class MigrationResult
{
    public string SourcePath { get; set; } = "";
    public int TargetClubId { get; set; }
    public bool DryRun { get; set; }
    public List<string> SourceTablesFound { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public int MembersRead { get; set; }
    public int MembersImported { get; set; }
    public int MembersSkipped { get; set; }
    public int AttendanceGroupsRead { get; set; }
    public int AttendanceGroupsImported { get; set; }
    public int AttendanceGroupsSkipped { get; set; }

    public bool Success => Errors.Count == 0;
    public string Summary =>
        $"Migration {(DryRun ? "(dry-run) " : "")}{(Success ? "OK" : "FAILED")}: " +
        $"Members {MembersImported} imported (read {MembersRead}, skipped {MembersSkipped}); " +
        $"AttendanceGroups {AttendanceGroupsImported} imported (read {AttendanceGroupsRead}, skipped {AttendanceGroupsSkipped}); " +
        $"Errors {Errors.Count}, Warnings {Warnings.Count}";
}