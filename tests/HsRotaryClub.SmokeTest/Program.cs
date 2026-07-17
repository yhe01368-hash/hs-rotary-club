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
