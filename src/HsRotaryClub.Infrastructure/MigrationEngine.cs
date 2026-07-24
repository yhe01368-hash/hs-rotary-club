using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Runtime.Versioning;
using System.Text;
using HsRotaryClub.Domain;
using Microsoft.EntityFrameworkCore;

namespace HsRotaryClub.Infrastructure;

/// <summary>
/// v0.15 — 從舊版 VB6 + Jet .mdb 遷移到新 SQLite (ClubId = 1)。
/// v0.59 — 支援 4 種 legacy table:
///     - TS81     → Member (社員基本資料,Big5 中文)
///     - MAT1     → AttendanceGroup (年度組別彙總,Big5 中文)
///     - MAT11    → (略過 — 只是每月每組彙總,不算 entity,僅供 debug)
///     - MAT11_1  → AttendanceRecord (例會出席明細,Big5 中文)
/// 任何 table 都用 GetOrdinal try-catch,欄位缺就 null/default。
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

        // Force Big5 codepage (950) for Chinese text in old VB6 .mdb
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var big5 = Encoding.GetEncoding(950);

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

            // 1. 社員 (TS81)
            if (tables.Contains("TS81"))
            {
                try
                {
                    var members = ReadTs81Members(conn, targetClubId, big5);
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
                    result.Errors.Add($"讀取社員表 (TS81) 失敗: {ex.Message}");
                }
            }
            else
            {
                result.Warnings.Add("找不到 'TS81' table,略過社員遷移");
            }

            // 2. 年度組別 (MAT1) - 每社員每年應/實/補 出席
            if (tables.Contains("MAT1"))
            {
                try
                {
                    var groups = ReadMat1Groups(conn, targetClubId, big5);
                    result.AttendanceGroupsRead = groups.Count;
                    if (!dryRun)
                    {
                        int added = 0, skipped = 0;
                        foreach (var g in groups)
                        {
                            var exists = target.AttendanceGroups.Any(x => x.ClubId == g.ClubId && x.Year == g.Year
                                && x.GroupName == g.GroupName && x.GroupLeaderCode == g.GroupLeaderCode
                                && x.GroupMemberCode == g.GroupMemberCode);
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
                    result.Errors.Add($"讀取年度組別表 (MAT1) 失敗: {ex.Message}");
                }
            }
            else
            {
                result.Warnings.Add("找不到 'MAT1' table,略過年度組別遷移");
            }

            // 3. 例會出席明細 (MAT11_1) — 用 batch SaveChanges 避免 SQLite parameter limit (75K rows)
            if (tables.Contains("MAT11_1"))
            {
                try
                {
                    var records = ReadMat11Records(conn, targetClubId, big5);
                    result.AttendanceRecordsRead = records.Count;
                    if (!dryRun)
                    {
                        const int BATCH = 1000;  // SQLite max ~32K params ÷ ~30 fields = ~1K rows
                        int added = 0, skipped = 0;
                        var seen = new HashSet<(int Year, DateTime Date, int Code)>();  // dedup in-batch
                        foreach (var r in records)
                        {
                            var key = (r.Year, r.MeetingDate, r.MemberCode);
                            if (!seen.Add(key)) { skipped++; continue; }
                            var exists = target.AttendanceRecords.Any(x => x.ClubId == r.ClubId
                                && x.Year == r.Year
                                && x.MeetingDate == r.MeetingDate
                                && x.MemberCode == r.MemberCode);
                            if (exists) { skipped++; continue; }
                            target.AttendanceRecords.Add(r);
                            added++;
                            if (added % BATCH == 0) target.SaveChanges();
                        }
                        target.SaveChanges();
                        result.AttendanceRecordsImported = added;
                        result.AttendanceRecordsSkipped = skipped;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"讀取出席明細表 (MAT11_1) 失敗: {ex.Message}");
                }
            }
            else
            {
                result.Warnings.Add("找不到 'MAT11_1' table,略過出席明細遷移");
            }

            // MAT11 只標記找到,不匯入(只是彙總)
            if (tables.Contains("MAT11"))
            {
                result.Warnings.Add("找到 'MAT11' (每月彙總),此版本不匯入 — 用 MAT1/MAT11_1 已涵蓋");
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

    /// <summary>v0.59: 讀 TS81 社員主檔(Big5 中文). 欄位寬鬆對應(社員編號/Code, 社友姓名/Name, etc.)</summary>
    [SupportedOSPlatform("windows")]
    public static List<Member> ReadTs81Members(OleDbConnection conn, int clubId, Encoding big5)
    {
        var result = new List<Member>();
        using var cmd = new OleDbCommand("SELECT * FROM [TS81]", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            try
            {
                var m = new Member
                {
                    ClubId = clubId,
                    Code = TryGetIntAny(reader, "社員編號", "Code", "MemberCode") ?? 0,
                    Name = DecodeBig5(TryGetStringAny(reader, "社友姓名", "Name", "MemberName"), big5) ?? "(無姓名)",
                    EnglishName = DecodeBig5(TryGetStringAny(reader, "英文名字", "EnglishName"), big5),
                    Birthday = TryGetDateOnlyAny(reader, "社友生日", "Birthday"),
                    IdNumber = DecodeBig5(TryGetStringAny(reader, "社友身証", "IdNumber", "IDNumber"), big5),
                    Mobile = DecodeBig5(TryGetStringAny(reader, "手機", "Mobile", "CellPhone"), big5),
                    Email = DecodeBig5(TryGetStringAny(reader, "電子信箱", "Email"), big5),
                    IsCurrent = false,  // default, set below based on 遷出原因
                    // New fields v0.59
                    SpouseName = DecodeBig5(TryGetStringAny(reader, "配偶姓名", "SpouseName"), big5),
                    WeddingAnniversary = TryGetDateOnlyAny(reader, "結婚日期", "WeddingAnniversary"),
                    EmployerName = DecodeBig5(TryGetStringAny(reader, "服務名稱", "EmployerName"), big5),
                    EmployerTitle = DecodeBig5(TryGetStringAny(reader, "服務職稱", "EmployerTitle"), big5),
                    EmployerAddress = DecodeBig5(TryGetStringAny(reader, "服務地址", "EmployerAddress"), big5),
                    HomeAddress = DecodeBig5(TryGetStringAny(reader, "住宅地址", "HomeAddress"), big5),
                    HomeZip = DecodeBig5(TryGetStringAny(reader, "住宅區號", "HomeZip"), big5),
                    EmployerZip = DecodeBig5(TryGetStringAny(reader, "服務區號", "EmployerZip"), big5),
                    GroupNo = DecodeBig5(TryGetStringAny(reader, "組別編號", "GroupNo"), big5),
                    Occupation = DecodeBig5(TryGetStringAny(reader, "職業分類C", "Occupation", "職業分類"), big5),
                    Rid = DecodeBig5(TryGetStringAny(reader, "RI_ID", "Rid"), big5),
                    ReferrerName = DecodeBig5(TryGetStringAny(reader, "介紹社友", "ReferrerName", "Referrer"), big5),
                };
                // 暫時刪除 = "已" 表示軟刪, 其他表示現任
                var tdel = DecodeBig5(TryGetStringAny(reader, "遷出原因", "LeaveReason"), big5);
                if (tdel == "退社" || tdel == "死亡" || tdel == "移民")
                {
                    m.IsCurrent = false;
                    m.LeaveReason = tdel;
                    var ld = TryGetDateOnlyAny(reader, "遷出日期", "LeaveDate");
                    if (ld.HasValue) m.LeaveDate = ld.Value;
                }
                else
                {
                    m.IsCurrent = true;
                }
                // 入社日期
                var jd = TryGetDateOnlyAny(reader, "入社日期", "JoinDate");
                if (jd.HasValue) m.JoinDate = jd.Value;
                result.Add(m);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MigrationEngine] 跳過 TS81 row: {ex.Message}");
            }
        }
        return result;
    }

    /// <summary>v0.59: 讀 MAT1 年度組別(Big5). 每社員每年應/實/補 出席次數.</summary>
    [SupportedOSPlatform("windows")]
    public static List<AttendanceGroup> ReadMat1Groups(OleDbConnection conn, int clubId, Encoding big5)
    {
        var result = new List<AttendanceGroup>();
        using var cmd = new OleDbCommand("SELECT * FROM [MAT1]", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            try
            {
                var year = TryGetIntAny(reader, "年度", "Year") ?? DateTime.Today.Year;
                var groupName = DecodeBig5(TryGetStringAny(reader, "組別", "GroupName"), big5) ?? "未分組";
                var memberCode = TryGetIntAny(reader, "社員號碼", "MemberCode") ?? 0;
                var shouldAttend = TryGetIntAny(reader, "應出席", "ShouldAttend") ?? 0;
                var actualAttend = TryGetIntAny(reader, "實出席", "ActualAttend") ?? 0;
                var makeup = TryGetIntAny(reader, "補出席", "MakeupAttend") ?? 0;
                // MAT1 每社員/組別一筆 — 我們用 GroupMemberCode = memberCode, GroupLeaderCode 用第一筆的社員號碼當 dummy
                // 實際意義:AttendanceGroup 是「組別 + 組長 + 組員 + 應/實/補」一條 record
                // 這裡 Leader=MemberCode (這樣資料能 import,但意義上不正確 — 需要 MAT1 + 額外的 group leader 對照)
                // v0.59 先能匯入,之後改
                var g = new AttendanceGroup
                {
                    ClubId = clubId,
                    Year = year,
                    GroupName = groupName,
                    GroupLeaderCode = memberCode,  // placeholder
                    GroupLeaderName = "",
                    GroupMemberCode = memberCode,
                    GroupMemberName = "",
                    ShouldAttend = shouldAttend,
                    ActualAttend = actualAttend,
                    MakeupAttend = makeup,
                    IsActive = true,
                };
                result.Add(g);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MigrationEngine] 跳過 MAT1 row: {ex.Message}");
            }
        }
        return result;
    }

    /// <summary>v0.59: 讀 MAT11_1 例會出席明細(Big5). 每社員每次例會出席紀錄.</summary>
    [SupportedOSPlatform("windows")]
    public static List<AttendanceRecord> ReadMat11Records(OleDbConnection conn, int clubId, Encoding big5)
    {
        var result = new List<AttendanceRecord>();
        using var cmd = new OleDbCommand("SELECT * FROM [MAT11_1]", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            try
            {
                var year = TryGetIntAny(reader, "年度", "Year") ?? DateTime.Today.Year;
                var meetingDate = TryGetDateOnlyAny(reader, "出席日期", "MeetingDate", "應出席日");
                if (!meetingDate.HasValue) continue;  // 沒日期 skip
                var memberCode = TryGetIntAny(reader, "社員編號", "MemberCode") ?? 0;
                if (memberCode == 0) continue;
                var reason = DecodeBig5(TryGetStringAny(reader, "缺席因素", "AbsenceReason"), big5);
                // 缺席因素: 空/0 = 出席, 1 = 缺席, 補出席日有值 = 補出席
                var makeupDate = TryGetDateOnlyAny(reader, "補出席日", "MakeupDate");
                AttendanceType type;
                if (makeupDate.HasValue) type = AttendanceType.Makeup;
                else if (reason == "1" || reason == "缺席") type = AttendanceType.Absent;
                else type = AttendanceType.Present;
                var r2 = new AttendanceRecord
                {
                    ClubId = clubId,
                    Year = year,
                    MeetingDate = meetingDate.Value.ToDateTime(TimeOnly.MinValue),
                    MemberCode = memberCode,
                    MemberName = "",
                    Type = type,
                    MakeupDate = makeupDate?.ToDateTime(TimeOnly.MinValue),
                    Remarks = DecodeBig5(TryGetStringAny(reader, "友社代號", "FriendlyClubCode"), big5),
                };
                result.Add(r2);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MigrationEngine] 跳過 MAT11_1 row: {ex.Message}");
            }
        }
        return result;
    }

    /// <summary>Big5 → UTF-8 解碼. null 直接回傳. 純 ASCII 直接回傳.</summary>
    private static string? DecodeBig5(string? s, Encoding big5)
    {
        if (string.IsNullOrEmpty(s)) return s;
        // 已解碼的 .NET string — 看起來是 mojibake 還是真 Chinese?
        // 簡化: 含有 U+4E00..U+9FFF (CJK Unified Ideographs) → 已是 Chinese,直接回傳
        // (因為 .NET OleDb 已用 Big5 自動解碼到 UTF-16 string)
        foreach (var c in s)
        {
            if (c >= 0x4E00 && c <= 0x9FFF) return s;
        }
        // 純 ASCII 或符號 → 直接回傳
        return s;
    }

    // 寬鬆讀欄位 (case-insensitive, 多個 aliases)
    private static int? TryGetIntAny(DbDataReader r, params string[] candidates)
    {
        foreach (var c in candidates)
        {
            var v = TryGetInt(r, c);
            if (v.HasValue) return v;
        }
        return null;
    }
    private static int TryGetIntAny(DbDataReader r, int defaultValue, params string[] candidates)
        => TryGetIntAny(r, candidates) ?? defaultValue;
    private static string? TryGetStringAny(DbDataReader r, params string[] candidates)
    {
        foreach (var c in candidates)
        {
            var v = TryGetString(r, c);
            if (v != null) return v;
        }
        return null;
    }
    private static bool TryGetBoolAny(DbDataReader r, params string[] candidates)
    {
        foreach (var c in candidates)
        {
            var v = TryGetString(r, c);
            if (v != null)
            {
                if (v == "是" || v == "True" || v == "1") return true;
                if (v == "否" || v == "False" || v == "0") return false;
            }
        }
        return false;
    }
    private static DateOnly? TryGetDateOnlyAny(DbDataReader r, params string[] candidates)
    {
        foreach (var c in candidates)
        {
            var v = TryGetDateOnly(r, c);
            if (v.HasValue) return v;
            // 試 string parse (legacy mdb 文字欄位)
            try
            {
                var ord = r.GetOrdinal(c);
                if (r.IsDBNull(ord)) continue;
                var raw = r.GetValue(ord);
                if (raw is string s && DateOnly.TryParse(s, out var d)) return d;
                if (raw is DateTime dt2) return DateOnly.FromDateTime(dt2);
            }
            catch { }
        }
        return null;
    }

    // original helpers
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
        catch { return null;
        }
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
    public int AttendanceRecordsRead { get; set; }
    public int AttendanceRecordsImported { get; set; }
    public int AttendanceRecordsSkipped { get; set; }

    public bool Success => Errors.Count == 0;
    public string Summary =>
        $"Migration {(DryRun ? "(dry-run) " : "")}{(Success ? "OK" : "FAILED")}: " +
        $"Members {MembersImported} imported (read {MembersRead}, skipped {MembersSkipped}); " +
        $"AttendanceGroups {AttendanceGroupsImported} imported (read {AttendanceGroupsRead}, skipped {AttendanceGroupsSkipped}); " +
        $"AttendanceRecords {AttendanceRecordsImported} imported (read {AttendanceRecordsRead}, skipped {AttendanceRecordsSkipped}); " +
        $"Errors {Errors.Count}, Warnings {Warnings.Count}";
}
