using HsRotaryClub.Domain;

namespace HsRotaryClub.Infrastructure;

/// <summary>
/// 開發/測試用的試算 seed。
/// 還原舊版畫面看到的 139 吳丞晏等社員, 不裝載真實歷史資料 (v0.2 之後再做 .mdb 遷移工具)。
/// </summary>
public static class SeedData
{
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

    /// <summary>如果 Members 表空, 寫入 DemoMembers。</summary>
    public static void SeedIfEmpty(RotaryDbContext db)
    {
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
    }
}
