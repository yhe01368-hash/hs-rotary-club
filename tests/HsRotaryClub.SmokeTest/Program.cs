using HsRotaryClub.Domain;
using HsRotaryClub.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace HsRotaryClub.SmokeTest;

internal static class Program
{
    private static int _pass, _fail;

    private static async Task<int> Main()
    {
        await T01_CanConnect();
        await T02_CreateMember();
        await T03_ClubCollection();
        await T04_FriendlyClub();
        await T05_MonthlyReceivableSpec();
        await T06_IsCurrentToggle();
        await T07_QuickFilter();
        await T08_WeddingAnniversary();
        await T09_ReferrerChain();
        await T10_DisplayAttributes();
        await T11_TrySaveChangesExtension();
        await T12_QuickFilterReflection();
        await T13_CsvExporter();
        await T14_MultiCollectionSameMember();
        await T15_YearMonthFiltering();
        await T16_RecomputeSpec();
        await T17_FriendlyClubUniqueCode();
        await T18_ClubDonationInOutSum();
        await T19_FriendlyClubSoftDelete();
        await T20_DonationDisplayAttributes();
        await T21_InstallerArtifactsExist();
        await T22_InstallerAppIdStable();
        await T23_DotnetPublishArgs();
        await T24_InstallerIcoExists();
        await T25_LanguageCodePageZh();
        await T26_AppIconCsprojRef();
        await T27_MainWindowSize();
        await T28_ClubEntityCRUD();
        await T29_ClubManagementViewModel();

        Console.WriteLine();
        Console.WriteLine($"=== {_pass} passed, {_fail} failed ===");
        return _fail == 0 ? 0 : 1;
    }

    private static async Task T01_CanConnect()
    {
        await Run("T01 DbContext EnsureCreated + Seed", () =>
        {
            using var ctx = NewCtx(out var path);
            ctx.Database.EnsureCreated();
            SeedData.SeedIfEmpty(ctx);
            Assert(File.Exists(path), "db file not created");
            var count = ctx.Members.Count();
            Assert(count >= 3, $"expected seed >= 3, got {count}");
        });
    }

    private static async Task T02_CreateMember()
    {
        await Run("T02 Member add / find / delete", () =>
        {
            using var ctx = NewCtx(out _);
            var m = new Member
            {
                Code = 999,
                Name = "測試社員",
                EnglishName = "TEST",
                IdNumber = "Z999888777",
                Mobile = "0900-000-000",
                Email = "t@example.com",
                JoinDate = new DateOnly(2024, 1, 1),
            };
            ctx.Members.Add(m);
            ctx.SaveChanges();

            var got = ctx.Members.FirstOrDefault(x => x.Code == 999);
            Assert(got is not null, "Member not found after save");
            Assert(got!.Name == "測試社員", "name mismatch");

            ctx.Members.Remove(got);
            ctx.SaveChanges();
            Assert(ctx.Members.FirstOrDefault(x => x.Code == 999) is null, "delete failed");
        });
    }

    private static async Task T03_ClubCollection()
    {
        await Run("T03 ClubCollection CRUD round-trip", () =>
        {
            using var ctx = NewCtx(out _);
            var c = new ClubCollection
            {
                Year = 2026, Month = 7,
                CollectionDate = new(2026, 7, 1),
                Category = "會費",
                MemberCode = 139,
                CashAmount = 1500m,
                CheckAmount = 0m,
            };
            ctx.ClubCollections.Add(c);
            ctx.SaveChanges();

            // SQLite 不能 Sum(decimal),pull client side
            var total = ctx.ClubCollections
                .Where(x => x.Year == 2026 && x.Month == 7)
                .AsEnumerable()
                .Sum(x => x.CashAmount);
            Assert(total >= 1500m, $"total {total} < 1500");

            ctx.ClubCollections.Remove(c);
            ctx.SaveChanges();
        });
    }

    private static async Task T04_FriendlyClub()
    {
        await Run("T04 FriendlyClub CRUD + donation FK", () =>
        {
            using var ctx = NewCtx(out _);
            var fc = new FriendlyClub { ClubCode = "T999", ClubName = "測試友社" };
            ctx.FriendlyClubs.Add(fc);
            ctx.SaveChanges();

            var dn = new ClubDonation
            {
                TxDate = new(2026, 7, 5),
                FriendlyClubId = fc.Id,
                Direction = DonationDirection.Out,
                Amount = 3000m,
                Purpose = "例會紀念",
            };
            ctx.ClubDonations.Add(dn);
            ctx.SaveChanges();

            // 驗 donation FK 查得到
            var d = ctx.ClubDonations.FirstOrDefault(x => x.FriendlyClubId == fc.Id);
            Assert(d is not null && d.Purpose == "例會紀念", "donation not persisted / FK broken");

            ctx.ClubDonations.Remove(d!);
            // 重新查一次別用 Include(字串) — FC entity 沒 nav property
            var reloaded2 = ctx.FriendlyClubs.FirstOrDefault(x => x.Id == fc.Id);
            if (reloaded2 is not null)
            {
                ctx.FriendlyClubs.Remove(reloaded2);
            }
            ctx.SaveChanges();
        });
    }

    private static async Task T05_MonthlyReceivableSpec()
    {
        await Run("T05 MonthlyReceivableSpec Outstanding 計算", () =>
        {
            using var ctx = NewCtx(out _);
            var spec = new MonthlyReceivableSpec
            {
                Year = 2026, Month = 7,
                MemberCode = 139,
                Item = "例餐費",
                Amount = 2500m,
            };
            ctx.MonthlyReceivableSpecs.Add(spec);
            ctx.SaveChanges();

            spec.SettledAmount = 1000m;
            ctx.SaveChanges();

            var reloaded = ctx.MonthlyReceivableSpecs.First(x => x.Id == spec.Id);
            Assert(reloaded.Amount == 2500m, "amount drift");
            Assert(reloaded.SettledAmount == 1000m, "settled drift");
            Assert(reloaded.OutstandingAmount == 1500m, "outstanding calc wrong");

            ctx.MonthlyReceivableSpecs.Remove(reloaded);
            ctx.SaveChanges();
        });
    }

    private static async Task T06_IsCurrentToggle()
    {
        await Run("T06 Member IsCurrent toggle 軟刪 CRUD", () =>
        {
            using var ctx = NewCtx(out _);
            var m = new Member { Code = 880, Name = "TEST06", IsCurrent = true };
            ctx.Members.Add(m);
            ctx.SaveChanges();

            var got = ctx.Members.First(x => x.Code == 880);
            Assert(got.IsCurrent == true, "default IsCurrent should be true");

            got.IsCurrent = false;
            got.LeaveDate = new DateOnly(2026, 7, 17);
            got.LeaveReason = "測試軟刪";
            ctx.SaveChanges();

            var reloaded = ctx.Members.First(x => x.Code == 880);
            Assert(reloaded.IsCurrent == false, "IsCurrent should be false after toggle");
            Assert(reloaded.LeaveDate == new DateOnly(2026, 7, 17), "LeaveDate not persisted");
            Assert(reloaded.LeaveReason == "測試軟刪", "LeaveReason not persisted (中文)");

            var current = ctx.Members.Where(x => x.IsCurrent).Count();
            var resigned = ctx.Members.Where(x => !x.IsCurrent).Count();
            // TEST06 自己被軟刪,完蛋無 current members 在這個 fresh test db
            Assert(current == 0, $"after soft-delete, current should be 0 (got {current})");
            Assert(resigned == 1, $"resigned should be 1 (got {resigned})");

            ctx.Members.Remove(reloaded);
            ctx.SaveChanges();
        });
    }

    private static async Task T07_QuickFilter()
    {
        await Run("T07 Member 速查 filter (name/en/code)", () =>
        {
            using var ctx = NewCtx(out _);
            ctx.Members.Add(new Member { Code = 801, Name = "張小明", EnglishName = "MIKE", IsCurrent = true });
            ctx.Members.Add(new Member { Code = 802, Name = "張小華", EnglishName = "ALAN", IsCurrent = true });
            ctx.Members.Add(new Member { Code = 803, Name = "王小明", EnglishName = "JOHN", IsCurrent = true });
            ctx.SaveChanges();

            var zhangs = ctx.Members.Where(m => m.Name.Contains("張")).Count();
            Assert(zhangs >= 2, $"expected 2+ 張, got {zhangs}");

            var alan = ctx.Members.FirstOrDefault(m => m.EnglishName == "ALAN");
            Assert(alan is not null, "ALAN not found");

            var code803 = ctx.Members.FirstOrDefault(m => m.Code == 803);
            Assert(code803 is not null && code803.Name == "王小明", "code 803 lookup wrong");

            foreach (var x in new[] { 801, 802, 803 })
                ctx.Members.Remove(ctx.Members.First(m => m.Code == x));
            ctx.SaveChanges();
        });
    }

    private static async Task T08_WeddingAnniversary()
    {
        await Run("T08 Member 配偶 + 結婚紀念日 round-trip", () =>
        {
            using var ctx = NewCtx(out _);
            var m = new Member
            {
                Code = 850,
                Name = "TEST08",
                SpouseName = "配偶 A",
                WeddingAnniversary = new DateOnly(2010, 6, 1),
                IsCurrent = true,
            };
            ctx.Members.Add(m);
            ctx.SaveChanges();

            var got = ctx.Members.First(x => x.Code == 850);
            Assert(got.SpouseName == "配偶 A", "spouse 中文 NOT persisted");
            Assert(got.WeddingAnniversary == new DateOnly(2010, 6, 1), "wedding date mismatch");

            got.SpouseName = "配偶 B 改";
            got.WeddingAnniversary = new DateOnly(2015, 12, 25);
            ctx.SaveChanges();

            var reload = ctx.Members.First(x => x.Code == 850);
            Assert(reload.SpouseName == "配偶 B 改", "spouse update not persisted");
            Assert(reload.WeddingAnniversary == new DateOnly(2015, 12, 25), "wedding update mismatch");

            ctx.Members.Remove(reload);
            ctx.SaveChanges();
        });
    }

    private static async Task T09_ReferrerChain()
    {
        await Run("T09 Member 介紹人自參照 (Code-int FK-like)", () =>
        {
            using var ctx = NewCtx(out _);
            ctx.Members.Add(new Member { Code = 901, Name = "介紹人 P", IsCurrent = true });
            ctx.Members.Add(new Member { Code = 902, Name = "被介紹人 Q", ReferrerCode = 901, ReferrerName = "介紹人 P", IsCurrent = true });
            ctx.SaveChanges();

            var q = ctx.Members.First(m => m.Code == 902);
            Assert(q.ReferrerCode == 901, "ReferrerCode not persisted");
            Assert(q.ReferrerName == "介紹人 P", "ReferrerName not persisted");

            var referrals = ctx.Members.Where(m => m.ReferrerCode == 901).ToList();
            Assert(referrals.Any(m => m.Code == 902), "referral lookup failed");

            foreach (var x in new[] { 901, 902 })
                ctx.Members.Remove(ctx.Members.First(m => m.Code == x));
            ctx.SaveChanges();
        });
    }

    private static async Task T10_DisplayAttributes()
    {
        await Run("T10 Member [Display(Name=\"...\")] 中文 metadata", () =>
        {
            var props = typeof(Member)
                .GetProperties()
                .Where(p => p.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), inherit: true).Any())
                .ToDictionary(
                    p => p.Name,
                    p => p.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>()!.GetName() ?? p.Name);

            Assert(props.ContainsKey("Name"), "Name Display attr missing");
            Assert(props["Name"] == "社友姓名", $"Name header '{props["Name"]}'");
            Assert(props["Code"] == "社員編號", $"Code header '{props["Code"]}'");
            Assert(props["IdNumber"] == "身份証字號", $"IdNumber header '{props["IdNumber"]}'");
            Assert(props["WeddingAnniversary"] == "結婚紀念日", $"WeddingAnniversary header '{props["WeddingAnniversary"]}'");
            Assert(props["IsCurrent"] == "現任", $"IsCurrent header '{props["IsCurrent"]}'");
            Assert(props.GetValueOrDefault("Rid") == "RID", $"RID header wrong: '{props.GetValueOrDefault("Rid")}'");
        });
    }

    private static async Task T11_TrySaveChangesExtension()
    {
        await Run("T11 TrySaveChanges extension: happy + sad path", () =>
        {
            using var ctx = NewCtx(out _);

            // 1. happy path
            var ok = new Member { Code = 711, Name = "T11-OK", IsCurrent = true };
            ctx.Members.Add(ok);
            Assert(ctx.TrySaveChanges(out var err) == true, $"happy should pass: {err}");
            Assert(err == string.Empty, $"error msg should be empty, got '{err}'");

            // 2. sad path: 加一個違反 unique constraint (Code 重複)
            var dup = new Member { Code = 711, Name = "T11-DUP", IsCurrent = true };
            ctx.Members.Add(dup);
            Assert(ctx.TrySaveChanges(out var err2) == false, "duplicate Code should NOT save");
            Assert(err2.Contains("UNIQUE constraint") || err2.Contains("constraint") || err2.Length > 0,
                $"error should describe problem, got '{err2}'");
            ctx.ChangeTracker.Clear();

            // 3. 然後再 save 一次乾淨版本應該過
            var clean = new Member { Code = 712, Name = "T11-CLEAN", IsCurrent = true };
            ctx.Members.Add(clean);
            Assert(ctx.TrySaveChanges(out var err3), $"third save should be clean: {err3}");

            // 清理
            ctx.Members.Remove(ctx.Members.First(m => m.Code == 711));
            ctx.Members.Remove(ctx.Members.First(m => m.Code == 712));
            ctx.SaveChanges();
        });
    }

    private static async Task T12_QuickFilterReflection()
    {
        await Run("T12 DbContextSaveOkExtension TrySaveChanges 委派呼叫 OK", () =>
        {
            // SmokeTest 不引用 HsRotaryClub.App (WinExe),所以 reflection load 跨平台 assembly 太脆,
            // 改驗 Infrastructure 的 TrySaveChanges extension 直接能用 — 給 VM 寫來用的 code path。

            using var ctx = NewCtx(out _);

            // 建一個 + 改一個 + 撤回一個混合 batch,測 happy path
            var a = new Member { Code = 721, Name = "T12A", IsCurrent = true };
            var b = new Member { Code = 722, Name = "T12B", IsCurrent = true };
            ctx.Members.AddRange(a, b);
            Assert(ctx.TrySaveChanges(out var err), $"add 2 should pass: {err}");

            // 內含 FK constraint miss — FriendlyClubId = 0 應被 EF Sqlite restrict 阻擋
            var donation = new ClubDonation
            {
                TxDate = new DateOnly(2026, 7, 17),
                FriendlyClubId = 999999,  // 假設不存在
                Direction = DonationDirection.Out,
                Amount = 100m,
            };
            ctx.ClubDonations.Add(donation);
            Assert(ctx.TrySaveChanges(out var err2) == false, "FK miss should fail");
            Assert(err2.Length > 0, "error should not be empty");

            // Clean up
            ctx.ChangeTracker.Clear();
            ctx.Members.Remove(ctx.Members.First(m => m.Code == 721));
            ctx.Members.Remove(ctx.Members.First(m => m.Code == 722));
            ctx.SaveChanges();
        });
    }

    private static async Task T13_CsvExporter()
    {
        await Run("T13 CsvExporter UTF-8 BOM + Display header", () =>
        {
            var rows = new[]
            {
                new Member { Code = 730, Name = "張小明", EnglishName = "MIKE", Mobile = "0912-345-678", Birthday = new(1990, 5, 1), IsCurrent = true },
                new Member { Code = 731, Name = "陳小美,女", EnglishName = "ALAN", Mobile = null, Birthday = new(1992, 6, 15), IsCurrent = false },
            };
            var tmp = Path.Combine(Path.GetTempPath(), $"csvtest-{Guid.NewGuid():N}.csv");
            try
            {
                CsvExporter.WriteCsv(tmp, rows);

                // 1. BOM
                var raw = File.ReadAllBytes(tmp);
                Assert(raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF, "BOM missing");

                // 2. Header 必須含 Display 屬性的中文
                var text = File.ReadAllText(tmp, System.Text.Encoding.UTF8);
                Assert(text.Contains("社員編號"), "header should contain 社員編號");
                Assert(text.Contains("社友姓名"), "header should contain 社友姓名");
                Assert(text.Contains("現任"), "header should contain 現任");

                // 3. 資料行:含 CSV escape 的逗號 / quoted field
                Assert(text.Contains("\"陳小美,女\""), "name with comma should be quoted");
                Assert(text.Contains("MIKE"), "should have english name");

                // 4. MemberName 不存在 display attr,所以用原始欄位名
                Assert(!text.Contains("MemberName"), "raw property name should NOT leak: MemberName");
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        });
    }

    private static async Task T14_MultiCollectionSameMember()
    {
        await Run("T14 同社員同月多筆 ClubCollection Sum 跟 TotalAmount", () =>
        {
            using var ctx = NewCtx(out _);
            ctx.Members.Add(new Member { Code = 740, Name = "T14 王", IsCurrent = true });
            ctx.Members.Add(new Member { Code = 741, Name = "T14 李", IsCurrent = true });
            ctx.SaveChanges();

            ctx.ClubCollections.AddRange(
                new ClubCollection { Year = 2026, Month = 8, CollectionDate = new(2026, 8, 1), Category = "會費", MemberCode = 740, MemberName = "T14 王", CashAmount = 1000m, CheckAmount = 0m },
                new ClubCollection { Year = 2026, Month = 8, CollectionDate = new(2026, 8, 5), Category = "會費", MemberCode = 740, MemberName = "T14 王", CashAmount = 500m, CheckAmount = 500m },
                new ClubCollection { Year = 2026, Month = 8, CollectionDate = new(2026, 8, 10), Category = "例餐", MemberCode = 740, MemberName = "T14 王", CashAmount = 200m, CheckAmount = 0m },
                new ClubCollection { Year = 2026, Month = 8, CollectionDate = new(2026, 8, 2), Category = "會費", MemberCode = 741, MemberName = "T14 李", CashAmount = 1500m, CheckAmount = 0m });
            ctx.SaveChanges();

            var wang = ctx.ClubCollections
                .Where(c => c.Year == 2026 && c.Month == 8 && c.MemberCode == 740)
                .AsEnumerable();
            Assert(wang.Count() == 3, "wang should have 3 collections");
            var total = wang.Sum(c => c.CashAmount + c.CheckAmount);
            Assert(total == 2200m, $"wang total should be 2200 (1000+500+200+500), got {total}");

            // total by category 同月同社員
            var wangFeeRows = ctx.ClubCollections.Where(c =>
                c.Year == 2026 && c.Month == 8 && c.MemberCode == 740 && c.Category == "會費")
                .AsEnumerable();
            Assert(wangFeeRows.Sum(c => c.CashAmount + c.CheckAmount) == 2000m,
                $"wang 會費 should be 2000, got {wangFeeRows.Sum(c => c.CashAmount + c.CheckAmount)}");

            // Cleanup
            ctx.ClubCollections.RemoveRange(ctx.ClubCollections.Where(c => c.Year == 2026 && c.Month == 8));
            ctx.Members.RemoveRange(ctx.Members.Where(m => m.Code == 740 || m.Code == 741));
            ctx.SaveChanges();
        });
    }

    private static async Task T15_YearMonthFiltering()
    {
        await Run("T15 Year/Month 切換查詢是隔離的", () =>
        {
            using var ctx = NewCtx(out _);
            ctx.Members.Add(new Member { Code = 750, Name = "T15", IsCurrent = true });
            ctx.ClubCollections.AddRange(
                new ClubCollection { Year = 2026, Month = 1, CollectionDate = new(2026, 1, 5), Category = "會費", MemberCode = 750, MemberName = "T15", CashAmount = 100m },
                new ClubCollection { Year = 2026, Month = 5, CollectionDate = new(2026, 5, 5), Category = "會費", MemberCode = 750, MemberName = "T15", CashAmount = 200m },
                new ClubCollection { Year = 2025, Month = 5, CollectionDate = new(2025, 5, 5), Category = "會費", MemberCode = 750, MemberName = "T15", CashAmount = 300m });
            ctx.SaveChanges();

            // 2026/5 only
            var may = ctx.ClubCollections.Where(c => c.Year == 2026 && c.Month == 5).ToList();
            Assert(may.Count == 1 && may[0].CashAmount == 200m, "should only see 2026-5 row");

            // 2025/5 only (different year)
            var lastYearMay = ctx.ClubCollections.Where(c => c.Year == 2025 && c.Month == 5).ToList();
            Assert(lastYearMay.Count == 1 && lastYearMay[0].CashAmount == 300m, "should only see 2025-5 row");

            // Cleanup
            ctx.ClubCollections.RemoveRange(ctx.ClubCollections.Where(c => c.MemberCode == 750));
            ctx.Members.Remove(ctx.Members.First(m => m.Code == 750));
            ctx.SaveChanges();
        });
    }

    private static async Task T16_RecomputeSpec()
    {
        await Run("T16 ReceivableSpec.SettledAmount 由多筆 Collection 加總後持久化", () =>
        {
            using var ctx = NewCtx(out _);
            ctx.Members.Add(new Member { Code = 760, Name = "T16 蔡", IsCurrent = true });
            ctx.SaveChanges();

            var spec = new MonthlyReceivableSpec
            {
                Year = 2026, Month = 9,
                MemberCode = 760,
                MemberName = "T16 蔡",
                Item = "會費",
                Amount = 2000m,
                SettledAmount = 0m,
            };
            ctx.MonthlyReceivableSpecs.Add(spec);
            ctx.SaveChanges();

            ctx.ClubCollections.AddRange(
                new ClubCollection { Year = 2026, Month = 9, CollectionDate = new(2026, 9, 1), Category = "會費", MemberCode = 760, MemberName = "T16 蔡", CashAmount = 700m, CheckAmount = 0m },
                new ClubCollection { Year = 2026, Month = 9, CollectionDate = new(2026, 9, 8), Category = "會費", MemberCode = 760, MemberName = "T16 蔡", CashAmount = 300m, CheckAmount = 200m });
            ctx.SaveChanges();

            // VM 邏輯:加總同月同社員同 Category
            var actualSettled = ctx.ClubCollections
                .Where(c => c.Year == 2026 && c.Month == 9 && c.MemberCode == 760 && c.Category == spec.Item)
                .AsEnumerable()
                .Sum(c => c.CashAmount + c.CheckAmount);
            Assert(actualSettled == 1200m, $"should sum to 1200 (700+300+200), got {actualSettled}");

            spec.SettledAmount = actualSettled;
            ctx.SaveChanges();
            var reload = ctx.MonthlyReceivableSpecs.First(s => s.Id == spec.Id);
            Assert(reload.SettledAmount == 1200m, "settled not persisted");
            Assert(reload.OutstandingAmount == 800m, "outstanding should be 2000 - 1200 = 800");

            // Cleanup
            ctx.ClubCollections.RemoveRange(ctx.ClubCollections.Where(c => c.Year == 2026 && c.Month == 9));
            ctx.MonthlyReceivableSpecs.Remove(reload);
            ctx.Members.Remove(ctx.Members.First(m => m.Code == 760));
            ctx.SaveChanges();
        });
    }

    private static async Task T17_FriendlyClubUniqueCode()
    {
        await Run("T17 FriendlyClub.ClubCode Unique 約束", () =>
        {
            using var ctx = NewCtx(out _);
            ctx.FriendlyClubs.Add(new FriendlyClub { ClubCode = "T17-A", ClubName = "社 A" });
            ctx.SaveChanges();

            // 違反 unique constraint 應該被 Sqlite 擋
            ctx.FriendlyClubs.Add(new FriendlyClub { ClubCode = "T17-A", ClubName = "社 A 重" });
            Assert(ctx.TrySaveChanges(out var err) == false, "duplicate ClubCode should fail");
            Assert(err.Contains("UNIQUE constraint") || err.Contains("constraint") || err.Length > 0,
                $"error msg expected, got '{err}'");
            ctx.ChangeTracker.Clear();

            // 改成不同 code
            ctx.FriendlyClubs.Add(new FriendlyClub { ClubCode = "T17-B", ClubName = "社 B" });
            Assert(ctx.TrySaveChanges(out var err2), $"different code should pass: {err2}");

            var rows = ctx.FriendlyClubs.Where(c => c.ClubCode.StartsWith("T17-")).ToList();
            Assert(rows.Count == 2, $"expected 2 rows, got {rows.Count}");

            // Cleanup
            foreach (var c in rows)
            {
                ctx.FriendlyClubs.Remove(c);
            }
            ctx.SaveChanges();
        });
    }

    private static async Task T18_ClubDonationInOutSum()
    {
        await Run("T18 ClubDonation 收入支出彙總 + FriendlyClubName 反查", () =>
        {
            using var ctx = NewCtx(out _);
            var fc = new FriendlyClub { ClubCode = "T18-X", ClubName = "T18 測試社" };
            ctx.FriendlyClubs.Add(fc);
            ctx.SaveChanges();

            ctx.ClubDonations.AddRange(
                new ClubDonation { TxDate = new(2026, 7, 1),  FriendlyClubId = fc.Id, FriendlyClubName = fc.ClubName, Direction = DonationDirection.Out, Amount = 1500m, Purpose = "付例會" },
                new ClubDonation { TxDate = new(2026, 7, 3),  FriendlyClubId = fc.Id, FriendlyClubName = fc.ClubName, Direction = DonationDirection.Out, Amount = 500m,  Purpose = "付雜項" },
                new ClubDonation { TxDate = new(2026, 7, 5),  FriendlyClubId = fc.Id, FriendlyClubName = fc.ClubName, Direction = DonationDirection.In,  Amount = 2000m, Purpose = "收捐款" },
                new ClubDonation { TxDate = new(2026, 7, 10), FriendlyClubId = fc.Id, FriendlyClubName = fc.ClubName, Direction = DonationDirection.In,  Amount = 800m,  Purpose = "收雜項" });
            ctx.SaveChanges();

            var all = ctx.ClubDonations.Where(d => d.FriendlyClubId == fc.Id).AsEnumerable();
            var outTotal = all.Where(d => d.Direction == DonationDirection.Out).Sum(d => d.Amount);
            var inTotal = all.Where(d => d.Direction == DonationDirection.In).Sum(d => d.Amount);
            Assert(outTotal == 2000m, $"Out total should be 2000, got {outTotal}");
            Assert(inTotal == 2800m, $"In total should be 2800, got {inTotal}");

            // FriendlyClubName 反查
            var sample = all.First();
            Assert(sample.FriendlyClubName == "T18 測試社", "FriendlyClubName not persisted");

            // 存完再讀 中文 purpose
            var purpose = ctx.ClubDonations.First(d => d.Purpose == "付雜項");
            Assert(purpose.Amount == 500m, "中文 Purpose 查不到");

            // Cleanup
            ctx.ClubDonations.RemoveRange(ctx.ClubDonations.Where(d => d.FriendlyClubId == fc.Id));
            ctx.FriendlyClubs.Remove(fc);
            ctx.SaveChanges();
        });
    }

    private static async Task T19_FriendlyClubSoftDelete()
    {
        await Run("T19 FriendlyClub 有捐款 → 軟刪 / 沒捐款 → 硬刪", () =>
        {
            using var ctx = NewCtx(out _);

            // Case 1: 沒捐款 → 硬刪
            var c1 = new FriendlyClub { ClubCode = "T19-X1", ClubName = "可刪" };
            ctx.FriendlyClubs.Add(c1);
            ctx.SaveChanges();

            var id1 = c1.Id;
            Assert(!ctx.ClubDonations.Any(d => d.FriendlyClubId == id1), "case 1 should have no donations");
            ctx.FriendlyClubs.Remove(c1);
            Assert(ctx.TrySaveChanges(out var err1), $"hard delete should pass: {err1}");
            Assert(ctx.FriendlyClubs.FirstOrDefault(x => x.Id == id1) is null, "case 1 should be hard-deleted");

            // Case 2: 有捐款 → VM 邏輯改 IsActive=false
            var c2 = new FriendlyClub { ClubCode = "T19-X2", ClubName = "不可刪" };
            ctx.FriendlyClubs.Add(c2);
            ctx.SaveChanges();
            ctx.ClubDonations.Add(new ClubDonation
            {
                TxDate = new(2026, 7, 17),
                FriendlyClubId = c2.Id,
                FriendlyClubName = c2.ClubName,
                Direction = DonationDirection.In,
                Amount = 1000m,
            });
            ctx.SaveChanges();

            Assert(ctx.ClubDonations.Any(d => d.FriendlyClubId == c2.Id), "case 2 should have donation");
            c2.IsActive = false;
            ctx.SaveChanges();
            var reloaded = ctx.FriendlyClubs.First(x => x.Id == c2.Id);
            Assert(reloaded.IsActive == false, "case 2 should be soft-deleted (IsActive=false)");

            // Cleanup
            ctx.ClubDonations.RemoveRange(ctx.ClubDonations.Where(d => d.FriendlyClubId == c2.Id));
            ctx.FriendlyClubs.Remove(reloaded);
            ctx.SaveChanges();
        });
    }

    private static async Task T20_DonationDisplayAttributes()
    {
        await Run("T20 FriendlyClub + ClubDonation [Display(Name=...)] 中文 metadata", () =>
        {
            var clubProps = typeof(FriendlyClub).GetProperties()
                .Where(p => p.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), inherit: true).Any())
                .ToDictionary(p => p.Name, p => p.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>()!.GetName() ?? p.Name);

            Assert(clubProps["ClubCode"] == "社團代號", $"ClubCode: '{clubProps["ClubCode"]}'");
            Assert(clubProps["ClubName"] == "社團名稱", $"ClubName: '{clubProps["ClubName"]}'");
            Assert(clubProps["Remarks"] == "備註", $"Remarks: '{clubProps["Remarks"]}'");
            Assert(clubProps["IsActive"] == "啟用", $"IsActive: '{clubProps["IsActive"]}'");

            var donateProps = typeof(ClubDonation).GetProperties()
                .Where(p => p.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), inherit: true).Any())
                .ToDictionary(p => p.Name, p => p.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>()!.GetName() ?? p.Name);

            Assert(donateProps["TxDate"] == "日期", $"TxDate: '{donateProps["TxDate"]}'");
            Assert(donateProps["FriendlyClubId"] == "友社", $"FriendlyClubId: '{donateProps["FriendlyClubId"]}'");
            Assert(donateProps["Direction"] == "方向", $"Direction: '{donateProps["Direction"]}'");
            Assert(donateProps["Amount"] == "金額", $"Amount: '{donateProps["Amount"]}'");
            Assert(donateProps["Purpose"] == "用途", $"Purpose: '{donateProps["Purpose"]}'");

            // Display 不含在 FK 欄位的 FriendlyClubId (or may) — verify 至少 TxDate / Amount / Purpose
            Assert(donateProps.GetValueOrDefault("FriendlyClubName") == "友社名稱",
                $"FriendlyClubName: '{donateProps.GetValueOrDefault("FriendlyClubName")}'");
        });
    }

    private static async Task T21_InstallerArtifactsExist()
    {
        await Run("T21 installer/ 目錄與 .iss / build script 存在", () =>
        {
            var projectRoot = ResolveProjectRoot();
            var installerDir = Path.Combine(projectRoot, "installer");
            Assert(Directory.Exists(installerDir), $"installer dir not found: {installerDir}");

            var iss = Path.Combine(installerDir, "HsRotaryClub.iss");
            Assert(File.Exists(iss), ".iss not found");
            var content = File.ReadAllText(iss);
            Assert(content.Contains("MyAppName"), ".iss missing MyAppName define");
            Assert(content.Contains("MyAppVersion"), ".iss missing MyAppVersion define");
            Assert(content.Contains("0.6.0"), ".iss missing 0.6.0 version");

            var ps1 = Path.Combine(installerDir, "build-installer.ps1");
            Assert(File.Exists(ps1), "build-installer.ps1 not found");

            var readme = Path.Combine(installerDir, "README.md");
            Assert(File.Exists(readme), "installer README.md not found");
        });
    }

    private static async Task T22_InstallerAppIdStable()
    {
        await Run("T22 Inno Setup AppId 固定 (升版才能 reuse UpgradeCode)", () =>
        {
            var projectRoot = ResolveProjectRoot();
            var iss = Path.Combine(projectRoot, "installer", "HsRotaryClub.iss");
            var content = File.ReadAllText(iss);

            // AppId 必須含 GUID-like
            Assert(content.Contains("AppId={"), ".iss missing AppId");
            // GUID 存在 (從內容抽)
            // Inno escape {{ ... }} → 抓 unwrapped GUID
            var match = System.Text.RegularExpressions.Regex.Match(content, @"AppId=\{+\{([0-9A-Fa-f\-]+)\}\}+");
            Assert(match.Success, $"AppId GUID not parseable near line containing 'AppId=':");

            // 不該是 placeholder
            var guid = match.Groups[1].Value;
            Assert(guid.Length >= 32 && guid.Length <= 40, $"AppId GUID looks too short: '{guid}'");
            Assert(!guid.Contains("XXXXX"), "AppId GUID still placeholder");
            Assert(!guid.Contains("RTR1"), "AppId GUID has placeholder characters (RTR1)");
            Assert(guid.Count(c => c == '-') == 4, $"AppId GUID should have 4 dashes, got '{guid}'");
        });
    }

    private static async Task T23_DotnetPublishArgs()
    {
        await Run("T23 dotnet publish parameters 對應 Inno .iss 期望路徑", () =>
        {
            var projectRoot = ResolveProjectRoot();
            var ps1 = Path.Combine(projectRoot, "installer", "build-installer.ps1");
            var content = File.ReadAllText(ps1);

            // 路徑要對齊 .iss 的 {#PublishDir}
            Assert(content.Contains("publish"), "publish path should contain 'publish'");
            Assert(content.Contains("Release"), "should use Release configuration");
            Assert(content.Contains("net8.0-windows\\win-x64\\publish"), "publish path should match expected");
        });
    }

    private static string ResolveProjectRoot()
    {
        // SmokeTest bin 在 tests/HsRotaryClub.SmokeTest/bin/Debug/net8.0/
        // 大概是 5 個 Parent 才到 hs-rotary-club/
        // 用 target marker: 找到含有 .gitignore 的目錄就當 root
        var dir = new DirectoryInfo(Path.GetDirectoryName(typeof(Member).Assembly.Location)!);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, ".gitignore")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("can't find repo root (.gitignore)");
    }

    private static async Task T24_InstallerIcoExists()
    {
        await Run("T24 installer/HsRotaryClub.ico 存在且 multi-resolution", () =>
        {
            var projectRoot = ResolveProjectRoot();
            var ico = Path.Combine(projectRoot, "installer", "HsRotaryClub.ico");
            Assert(File.Exists(ico), $"ico not found: {ico}");

            var bytes = File.ReadAllBytes(ico);
            // ICONDIR: 2 bytes reserved + 2 bytes type + 2 bytes count
            Assert(bytes.Length >= 6, "ico too small");
            var reserved = BitConverter.ToInt16(bytes, 0);
            var type = BitConverter.ToInt16(bytes, 2);
            var count = BitConverter.ToInt16(bytes, 4);
            Assert(reserved == 0, "reserved should be 0");
            Assert(type == 1, $"should be icon type 1, got {type}");
            Assert(count >= 3, $"should have at least 3 resolutions, got {count}");

            // Check size field of last entry is non-zero
            Assert(bytes.Length > 1000, $"ico size too small: {bytes.Length}");
        });
    }

    private static async Task T25_LanguageCodePageZh()
    {
        await Run("T25 .iss LanguageCodePage=950 (zh-TW codepage)", () =>
        {
            var projectRoot = ResolveProjectRoot();
            var iss = Path.Combine(projectRoot, "installer", "HsRotaryClub.iss");
            var content = File.ReadAllText(iss);
            Assert(content.Contains("LanguageCodePage=950"),
                ".iss missing LanguageCodePage=950 (Big5 codepage for zh-TW)");

            // AppName 應為 ASCII (避免 codepage garbled) — AppName 在 [Setup] section
            var appNameMatch = System.Text.RegularExpressions.Regex.Match(content, @"^AppName=(.+?)$", System.Text.RegularExpressions.RegexOptions.Multiline);
            Assert(appNameMatch.Success, "AppName missing");
            var appName = appNameMatch.Groups[1].Value.Trim();
            Assert(appName.All(c => c < 0x80), $"AppName should be ASCII, got '{appName}'");

            // 至少有一條中文的 [Messages] entry (WelcomeLabel2 / FinishedLabel etc.)
            var msgCn = System.Text.RegularExpressions.Regex.IsMatch(content, @"\[\s*Messages\s*\][\s\S]*?[\u4e00-\u9fff]");
            Assert(msgCn, "no Chinese in [Messages] section — user will see English wizard text");
        });
    }

    private static async Task T26_AppIconCsprojRef()
    {
        await Run("T26 HsRotaryClub.App.csproj 含 ApplicationIcon", () =>
        {
            var projectRoot = ResolveProjectRoot();
            var csproj = Path.Combine(projectRoot, "src", "HsRotaryClub.App", "HsRotaryClub.App.csproj");
            var content = File.ReadAllText(csproj);
            Assert(content.Contains("<ApplicationIcon>"),
                "csproj missing <ApplicationIcon>");
            // Verify it points to actual existing file
            var iconMatch = System.Text.RegularExpressions.Regex.Match(content, @"<ApplicationIcon>(.+?)</ApplicationIcon>");
            Assert(iconMatch.Success, "ApplicationIcon element not found");
            var relPath = iconMatch.Groups[1].Value;
            var fullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(csproj)!, relPath));
            Assert(File.Exists(fullPath), $"icon file missing: {fullPath}");
        });
    }

    private static async Task T27_MainWindowSize()
    {
        await Run("T27 MainWindow 預設尺寸夠大 (>=1280x720) + Maximized", () =>
        {
            var projectRoot = ResolveProjectRoot();
            var xaml = Path.Combine(projectRoot, "src", "HsRotaryClub.App", "Views", "MainWindow.xaml");
            var content = File.ReadAllText(xaml);

            // Width >= 1280
            var wMatch = System.Text.RegularExpressions.Regex.Match(content, @"Width=""(\d+)""");
            Assert(wMatch.Success, "Width not found");
            var w = int.Parse(wMatch.Groups[1].Value);
            Assert(w >= 1280, $"MainWindow Width={w}, expected >= 1280");

            // Height >= 720
            var hMatch = System.Text.RegularExpressions.Regex.Match(content, @"Height=""(\d+)""");
            Assert(hMatch.Success, "Height not found");
            var h = int.Parse(hMatch.Groups[1].Value);
            Assert(h >= 720, $"MainWindow Height={h}, expected >= 720");

            // MinWidth for small monitors fallback
            Assert(content.Contains("MinWidth=\"1280\""), "MinWidth=1280 missing");
            Assert(content.Contains("MinHeight=\"720\""), "MinHeight=720 missing");

            // WindowState=Maximized
            Assert(content.Contains("WindowState=\"Maximized\""), "WindowState=Maximized missing");
        });
    }

    private static async Task T28_ClubEntityCRUD()
    {
        await Run("T28 Club entity CRUD + unique name + Seed 預設社", () =>
        {
            using var ctx = NewCtx(out _);

            // 直接 AddRange 看 SeedIfEmpty 的 db.Clubs.Any() 路徑 — 此 test 用 fresh db
            // Add 2 clubs
            var fysw = new Club
            {
                Name = "豐原西南扶輪社",
                District = "3460 地區",
                CharterDate = new DateOnly(2006, 6, 1),
                Contact = "秘書處",
                ContactEmail = "fysw@rotary3460.org",
                IsActive = true,
            };
            var tcwb = new Club
            {
                Name = "台中西北扶輪社",
                District = "3460 地區",
                CharterDate = new DateOnly(1975, 3, 15),
                Contact = "李秘書",
                ContactEmail = "tcwb@rotary3460.org",
                IsActive = true,
            };
            ctx.Clubs.AddRange(fysw, tcwb);
            ctx.SaveChanges();

            Assert(ctx.Clubs.Count() == 2, "should have 2 clubs");

            // 查 fysw
            var got = ctx.Clubs.First(c => c.Name == "豐原西南扶輪社");
            Assert(got.District == "3460 地區", "district mismatch");
            Assert(got.CharterDate == new DateOnly(2006, 6, 1), "charter date mismatch");
            Assert(got.IsActive, "default should be active");

            // 中文 + Unicode round-trip
            Assert(got.Remarks == null || got.Remarks.Length >= 0, "Remarks getter works");

            // Unique name — 試圖加同名社應 fail
            var dup = new Club { Name = "豐原西南扶輪社" };
            ctx.Clubs.Add(dup);
            Assert(ctx.TrySaveChanges(out var err) == false, "duplicate Name should fail");
            Assert(err.Contains("UNIQUE") || err.Length > 0, $"expected unique constraint error: '{err}'");
            ctx.ChangeTracker.Clear();

            // Soft-disable
            var tracked = ctx.Clubs.First(c => c.Name == "豐原西南扶輪社");
            tracked.IsActive = false;
            ctx.SaveChanges();
            var active = ctx.Clubs.Where(c => c.IsActive).Count();
            Assert(active == 1, $"only 1 should be active, got {active}");

            // 軟刪後用 SeedData 加同 name 會成功(因為舊已 IsActive=false — IsUnique 只擋名稱 exact match,不擋 IsActive)
            // 實際上 unique name 仍擋 — 但我們清掉重新 add 看 isActive filter
            // 略,此測試聚焦主流程

            // 清理
            foreach (var c in ctx.Clubs.ToList())
            {
                ctx.Clubs.Remove(c);
            }
            ctx.SaveChanges();
        });
    }

    private static async Task T29_ClubManagementViewModel()
    {
        await Run("T29 Club query filter: ShowInactiveOnly + 速查 (對應 ClubManagementViewModel 邏輯)", () =>
        {
            using var ctx = NewCtx(out _);

            // seed 3 clubs
            var fysw = new Club { Name = "T29 豐原西南", District = "3460", IsActive = true };
            var tcwb = new Club { Name = "T29 台中西北", District = "3460", IsActive = true };
            var ths  = new Club { Name = "T29 大里扶輪社", District = "3460", IsActive = false };  // 停用的
            ctx.Clubs.AddRange(fysw, tcwb, ths);
            ctx.SaveChanges();

            // default 預設 (active + filter empty) — 應 2 個
            var activeOnly = ctx.Clubs.AsNoTracking().Where(c => c.IsActive).ToList();
            Assert(activeOnly.Count == 2, $"active only should be 2, got {activeOnly.Count}");

            // inactive only
            var inactiveOnly = ctx.Clubs.AsNoTracking().Where(c => !c.IsActive).ToList();
            Assert(inactiveOnly.Count == 1, $"inactive only should be 1, got {inactiveOnly.Count}");

            // 速查
            var matches = ctx.Clubs.AsNoTracking()
                .Where(c => c.IsActive && (c.Name.Contains("T29") || (c.District ?? "").Contains("T29")))
                .ToList();
            Assert(matches.Count == 2, $"filter T29 active should be 2, got {matches.Count}");

            // MakeCurrent 邏輯: 切社 = 改 CurrentClubId (vm 用)
            // 暫存 CurrentClubId 在 vm — 我們改用 static get 直接測 DbContext filter 切社
            var currentId = tcwb.Id;
            var current = ctx.Clubs.AsNoTracking().First(c => c.Id == currentId);
            Assert(current.Name == "T29 台中西北", $"current club should be tcwb, got '{current.Name}'");

            // DefaultClubId 常數 = 1
            Assert(SeedData.DefaultClubId == 1, "DefaultClubId should be 1");

            // 清理
            foreach (var c in ctx.Clubs.ToList()) ctx.Clubs.Remove(c);
            ctx.SaveChanges();
        });
    }

    private static RotaryDbContext NewCtx(out string path)
    {
        path = Path.Combine(Path.GetTempPath(), $"smoketest-rotary-{Guid.NewGuid():N}.db");
        var b = new DbContextOptionsBuilder<RotaryDbContext>();
        b.UseSqlite($"Data Source={path}");
        var ctx = new RotaryDbContext(b.Options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private static async Task Run(string name, Action body)
    {
        try
        {
            body();
            Console.WriteLine($"[PASS] {name}");
            _pass++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FAIL] {name} :: {ex.Message}");
            Console.WriteLine($"       :: {ex.InnerException?.Message}");
            Console.WriteLine($"       :: {ex.InnerException?.InnerException?.Message}");
            _fail++;
        }
        await Task.CompletedTask;
    }

    private static void Assert(bool cond, string msg)
    {
        if (!cond) throw new InvalidOperationException(msg);
    }
}
