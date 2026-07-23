using HsRotaryClub.Domain;

namespace HsRotaryClub.Infrastructure;

/// <summary>
/// 開發/測試用的試算 seed。
/// v0.7 開始每個 demo 資料帶 ClubId FK — 第一個 Club 「示範扶輪社」當預設社。
/// </summary>
public static class SeedData
{
    public static readonly Club[] DemoClubs =
    {
        new()
        {
            Name = "示範扶輪社",
            District = "3460 地區",
            CharterDate = new(2006, 6, 1),
            Contact = "秘書處",
            ContactEmail = "fysw@rotary3460.org",
            Remarks = "預設 demo 社團 (對應舊版 VB6 系統)",
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        },
    };

    public static readonly Member[] DemoMembers =
    {
        new() { Code = 121, Name = "呂維國", EnglishName = "LU-WEI-KUO",  Birthday = new(1965, 3, 12), IdNumber = "L123456789", Occupation = "營造業", Mobile = "0912-345-678", Email = "lu@example.com", Rid = "10000021", GroupNo = "0401", JoinDate = new(2018, 7, 1) },
        new() { Code = 124, Name = "劉增郎", EnglishName = "LIU-TSENG-LANG", Birthday = new(1972, 8, 5), IdNumber = "L123456790", Occupation = "建築師", Mobile = "0933-111-222", Email = "liu@example.com", Rid = "10000024", GroupNo = "0402", JoinDate = new(2019, 1, 12) },
        new() { Code = 139, Name = "吳丞晏", EnglishName = "WU-CHENG-YAN", Birthday = new(1997, 7, 23), IdNumber = "L125295578", Occupation = "不動產仲介", EmployerName = "台慶不動產", EmployerTitle = "業務專員", EmployerAddress = "臺中市北屯區崇德五路367號", EmployerZip = "406022", HomeAddress = "臺中市豐原區成功路600號", HomeZip = "420014", GroupNo = "0410", JoinDate = new(2024, 12, 5), Mobile = "0923-658-786", Email = "hans07231997@gmail.com", Rid = "12213492", SortOrder = 1 },
    };

    public static readonly FriendlyClub[] DemoFriendlyClubs =
    {
        new() { ClubCode = "FC001", ClubName = "台中西北扶輪社", Remarks = "例會紀念互通" },
        new() { ClubCode = "FC002", ClubName = "豐原扶輪社",     Remarks = "聯合理事會" },
        new() { ClubCode = "FC003", ClubName = "大里扶輪社",     Remarks = "聯合例會" },
    };

    /// <summary>v0.38 — 預設 admin 帳號.密碼用 PBKDF2-SHA256 雜湊後存.</summary>
    public static readonly User[] DemoUsers =
    {
        new()
        {
            Username = "admin",
            PasswordHash = PasswordHasher.Hash("admin"),
            DisplayName = "系統管理員",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        },
    };

    /// <summary>
    /// 預設社 ID = 1 (示範扶輪社)。所有 demo Member/FriendlyClub/ClubCollection
    /// 之後在 v0.7 進階 commit 加 ClubId = DefaultClubId 過濾。
    /// </summary>
    public const int DefaultClubId = 1;

    /// <summary>如果 Clubs / Members / FriendlyClubs 表空, 寫入 demo。</summary>
    public static void SeedIfEmpty(RotaryDbContext db)
    {
        if (!db.Clubs.Any())
        {
            db.Clubs.AddRange(DemoClubs);
            db.SaveChanges();
        }

        if (!db.Members.Any())
        {
            db.Members.AddRange(DemoMembers);
            db.SaveChanges();
        }

        if (!db.FriendlyClubs.Any())
        {
            db.FriendlyClubs.AddRange(DemoFriendlyClubs);
            db.SaveChanges();
        }

        // v0.38: 預設 admin 帳號 (Username=admin, Password=admin) — Admin 自己改成更安全的密碼.
        if (!db.Users.Any())
        {
            db.Users.AddRange(DemoUsers);
            db.SaveChanges();
        }

        // v0.55: 啟動時跑一次 legacy seed name 遷移 (把舊 db 的「豐原西南扶輪社」改成 generic「示範扶輪社」)
        MigrateLegacySeedNames(db);
    }

    /// <summary>v0.55: 把 db 內舊版的「豐原西南扶輪社」seed name 改成 generic「示範扶輪社」,避免多筆重名.</summary>
    private const string LegacySeedName = "豐原西南扶輪社";
    private const string GenericSeedName = "示範扶輪社";
    public static void MigrateLegacySeedNames(RotaryDbContext db)
    {
        try
        {
            var targets = db.Clubs.Where(c => c.Name == LegacySeedName).ToList();
            if (targets.Count == 0) return;
            foreach (var c in targets)
            {
                c.Name = GenericSeedName;
            }
            db.SaveChanges();
        }
        catch
        {
            // best-effort,don't crash startup if migration fails
        }
    }
}

