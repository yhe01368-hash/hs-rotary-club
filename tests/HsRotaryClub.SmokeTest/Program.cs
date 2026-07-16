using HsRotaryClub.Domain;
using HsRotaryClub.Infrastructure;
using Microsoft.EntityFrameworkCore;

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
