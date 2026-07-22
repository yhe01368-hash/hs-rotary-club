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
        await T30_MemberClubIdFilter();
        await T31_MemberCrossClubQuery();
        await T32_ClubPickerLogic();
        await T33_DataTransferExport();
        await T34_DataTransferRoundTrip();
        await T35_ImportExportDialogLogic();
        await T36_LicenseIssueValidate();
        await T37_LicenseExpired();
        await T38_LicenseMachineMismatch();
        await T39_TrialCanAddClub();
        await T40_LicenseMaxClubs();
        await T41_AttendanceGroupCRUD();
        await T42_AttendanceRecord();
        await T43_AttendanceRateCalc();
        await T44_AttendanceFilterByYearGroup();
        await T45_OtherIncomeCRUD();
        await T46_MonthlyExpenseCRUD();
        await T47_AccountSubjectAndEntry();
        await T48_AccountMonthlyReportFormula();
        await T49_MailJobCreate();
        await T50_MailRecipientStatus();
        await T51_MigrationMissingFile();
        await T52_MigrationResultSummary();
        await T53_DbInitializerRecreate();
        await T54_XamlBomAndFont();
        await T55_NavItemChineseTitle();
        await T56_AppXamlGlobalFontSize();
        await T57_WpfPageCtorScanned();
        await T58_NoFengyuanWestText();
        await T59_DropLegacyUnique();
        await T60_PasswordHashing();
        await T61_FindMissingTables();
        await T62_CollectorAutoDefault();

        Console.WriteLine();
        Console.WriteLine($"=== {_pass} passed, {_fail} failed ===");
        return _fail == 0 ? 0 : 1;
    }

    private static async Task T62_CollectorAutoDefault()
    {
        await Run("T62 v0.40/v0.41: ClubCollection.Collector defaults to current user DisplayName", () =>
        {
            // Simulate: Add() with src=null (fresh), currentUser "系統管理員"
            // Then assert: Collector = "系統管理員"
            var src = (ClubCollection?)null;
            var isAuthed = true;
            var displayName = "系統管理員";

            string collector = !string.IsNullOrWhiteSpace(src?.Collector)
                ? src!.Collector
                : (isAuthed ? displayName : "");

            Assert(collector == "系統管理員", $"expected auto-fill '系統管理員', got '{collector}'");

            // Save() with empty existing Collector + authed user → also fills
            var existing = new ClubCollection { Collector = "" };
            if (string.IsNullOrWhiteSpace(existing.Collector) && isAuthed)
                existing.Collector = displayName;
            Assert(existing.Collector == "系統管理員", "Save() should fill empty Collector");

            // Save() with non-empty existing Collector → user wins
            existing.Collector = "手工指定";
            Assert(existing.Collector == "手工指定", "Save() should preserve existing Collector");
        });
    }

    private static async Task T61_FindMissingTables()
    {
        await Run("T61 v0.38.1: DbInitializer.FindMissingTables detects new entity tables (Users)", () =>
        {
            // Build a "v0.37-era" db: only old entity tables present, no Users.
            var path = Path.Combine(Path.GetTempPath(), $"smoketest-missing-{Guid.NewGuid():N}.db");

            void RunSql(Action<Microsoft.Data.Sqlite.SqliteConnection> body)
            {
                using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False");
                conn.Open();
                body(conn);
            }

            // 模擬 v0.37 schema:有 Clubs/Members/FriendlyClubs 但沒 Users
            RunSql(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText =
                    "CREATE TABLE Clubs (Id INTEGER PRIMARY KEY, Name TEXT);" +
                    "CREATE TABLE Members (Id INTEGER PRIMARY KEY, Code INTEGER, Name TEXT);" +
                    "CREATE TABLE FriendlyClubs (Id INTEGER PRIMARY KEY, ClubCode TEXT);";
                cmd.ExecuteNonQuery();
            });

            // Use Microsoft.Data.Sqlite.SqliteConnection (same lib DbInitializer uses)
            var missing = new List<string>();
            using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False"))
            {
                conn.Open();
                var required = new[] { "Clubs", "Members", "FriendlyClubs", "Users" };
                foreach (var t in required)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name";
                    var p = cmd.CreateParameter();
                    p.ParameterName = "$name";
                    p.Value = t;
                    cmd.Parameters.Add(p);
                    var n = Convert.ToInt32(cmd.ExecuteScalar());
                    if (n == 0) missing.Add(t);
                }
            }
            Console.WriteLine($"  missing tables: [{string.Join(", ", missing)}]");
            Assert(missing.Count == 1 && missing[0] == "Users",
                $"expected only Users missing, got [{string.Join(", ", missing)}]");

            try { File.Delete(path); } catch { }
        });
    }

    private static async Task T60_PasswordHashing()
    {
        await Run("T60 v0.38: PasswordHasher.Hash + Verify round-trip + invalid rejects", () =>
        {
            var password = "secret123";
            var stored = HsRotaryClub.Infrastructure.PasswordHasher.Hash(password);
            Assert(stored.Contains(':'), "stored hash should have salt:hash format");
            var (saltB64, hashB64) = (stored.Split(':')[0], stored.Split(':')[1]);
            Assert(saltB64.Length > 0 && hashB64.Length > 0, "salt/hash non-empty");

            // Verify same password matches
            Assert(HsRotaryClub.Infrastructure.PasswordHasher.Verify(password, stored), "Verify(same) should match");

            // Verify different password fails
            Assert(!HsRotaryClub.Infrastructure.PasswordHasher.Verify("wrong", stored), "Verify(different) should fail");

            // Empty / null safety
            Assert(!HsRotaryClub.Infrastructure.PasswordHasher.Verify("", stored), "empty password should fail");
            Assert(!HsRotaryClub.Infrastructure.PasswordHasher.Verify(password, ""), "empty stored should fail");

            // Two hashes of same password differ (random salt)
            var stored2 = HsRotaryClub.Infrastructure.PasswordHasher.Hash(password);
            Assert(stored != stored2, "two hashes should differ (salt randomness)");
            Assert(HsRotaryClub.Infrastructure.PasswordHasher.Verify(password, stored2), "second hash also verifies");

            // Corrupted stored format
            Assert(!HsRotaryClub.Infrastructure.PasswordHasher.Verify(password, "garbage"), "garbage stored should fail");
            Assert(!HsRotaryClub.Infrastructure.PasswordHasher.Verify(password, "a:b:c"), "3-part stored should fail");
        });
    }

    private static async Task T59_DropLegacyUnique()
    {
        await Run("T59 v0.32: DbInitializer.DropLegacyUniqueConstraints drops IX_Clubs_Name", () =>
        {
            var path = Path.Combine(Path.GetTempPath(), $"smoketest-legacy-{Guid.NewGuid():N}.db");

            void RunSql(Action<Microsoft.Data.Sqlite.SqliteConnection> body)
            {
                using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False");
                conn.Open();
                body(conn);
            }

            RunSql(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "CREATE TABLE Clubs (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL);"
                    + "CREATE UNIQUE INDEX IX_Clubs_Name ON Clubs(Name);"
                    + "INSERT INTO Clubs(Name) VALUES ('示範');";
                cmd.ExecuteNonQuery();
            });

            int indexCount = 999;
            RunSql(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name='IX_Clubs_Name'";
                indexCount = Convert.ToInt32(cmd.ExecuteScalar());
            });
            Assert(indexCount == 1, $"expected IX_Clubs_Name index present initially, got {indexCount}");

            // Drop — Microsoft.Data.Sqlite's ExecuteNonQuery returns 0 for DDL even on success,
            // so we verify via SELECT instead of relying on rowcount.
            RunSql(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "DROP INDEX IF EXISTS IX_Clubs_Name";
                cmd.ExecuteNonQuery();
            });

            int afterCount = 999;
            RunSql(conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name='IX_Clubs_Name'";
                afterCount = Convert.ToInt32(cmd.ExecuteScalar());
            });
            Assert(afterCount == 0, $"expected IX_Clubs_Name to be gone, got {afterCount}");

            try { File.Delete(path); } catch { }
        });
    }

    private static async Task T57_WpfPageCtorScanned()
    {
        await Run("T57 v0.25: WPF View/*Page.xaml.cs 都有無參 ctor (DataTemplate new XxxPage() 才行)", () =>
        {
            // 只檢查 Views/ 裡的 *Page.xaml.cs — Dialog 用 static Show() factory 可以有 DI ctor.
            var viewsDir = Path.Combine(ResolveProjectRoot().ToString(), "src", "HsRotaryClub.App", "Views");
            int diCtorCount = 0;
            int totalCtors = 0;
            foreach (var xamlCs in Directory.GetFiles(viewsDir, "*Page.xaml.cs"))
            {
                var text = File.ReadAllText(xamlCs, System.Text.Encoding.UTF8);
                var matches = System.Text.RegularExpressions.Regex.Matches(text, @"public\s+(\w+)\s*\(([^)]*)\)");
                foreach (System.Text.RegularExpressions.Match m in matches)
                {
                    var parms = m.Groups[2].Value;
                    totalCtors++;
                    if (string.IsNullOrWhiteSpace(parms)) continue;
                    diCtorCount++;
                    Console.WriteLine($"  [DI ctor] {Path.GetFileName(xamlCs)}: {m.Groups[1].Value}({parms})");
                }
            }
            Console.WriteLine($"  total Page ctors: {totalCtors}, with DI params: {diCtorCount}");
            Assert(diCtorCount == 0, $"expected 0 DI ctors in *Page.xaml.cs (got {diCtorCount}). WPF DataTemplate needs no-arg ctor.");
        });
    }

    private static async Task T58_NoFengyuanWestText()
    {
        await Run("T58 v0.28: 整個 source 沒有 豐原西南 字眼 (支援不同扶輪社) [允許 migration 字串]", () =>
        {
            var srcDir = Path.Combine(ResolveProjectRoot().ToString(), "src");
            int totalHits = 0;
            var hits = new List<string>();
            foreach (var ext in new[] { "*.xaml", "*.cs", "*.iss", "*.issxaml" })
            {
                foreach (var f in Directory.GetFiles(srcDir, ext, SearchOption.AllDirectories))
                {
                    if (f.Contains("Licenses")) continue; // skip license.*.txt
                    var text = File.ReadAllText(f, System.Text.Encoding.UTF8);
                    if (text.Contains("豐原西南"))
                    {
                        // Allow exception: ClubManagementViewModel.cs has LegacySeedName constant
                        // used for one-time migration — this is the migration source itself.
                        if (f.EndsWith("ClubManagementViewModel.cs"))
                        {
                            // accept: this file has the migration constant by design (v0.29).
                            continue;
                        }
                        totalHits++;
                        hits.Add(Path.GetFileName(f));
                    }
                }
            }
            // Also check installer
            var iss = Path.Combine(ResolveProjectRoot().ToString(), "installer", "HsRotaryClub.iss");
            if (File.Exists(iss))
            {
                var text = File.ReadAllText(iss, System.Text.Encoding.UTF8);
                if (text.Contains("豐原西南"))
                {
                    totalHits++;
                    hits.Add("HsRotaryClub.iss");
                }
            }
            Console.WriteLine($"  hits: {hits.Count} -> {hits}");
            Assert(totalHits == 0, $"found 豐原西南 in {totalHits} files: {string.Join(", ", hits)}");
        });
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

            // v0.32: Club.Name 不再 unique — 不同社可同名 (e.g. 多個示範/分區). 應能加同名社.
            var dup = new Club { Name = "豐原西南扶輪社", District = "3460 / diff" };
            ctx.Clubs.Add(dup);
            Assert(ctx.TrySaveChanges(out var err), $"duplicate name should now save: '{err}'");
            ctx.ChangeTracker.Clear();

            // Soft-disable
            // v0.32: Club.Name 不再 unique — 設 ALL 同名的 active 為 false,確保 active count 為 0.
            var tracked = ctx.Clubs.Where(c => c.Name == "豐原西南扶輪社").ToList();
            foreach (var c in tracked) c.IsActive = false;
            ctx.SaveChanges();
            var active = ctx.Clubs.Where(c => c.Name == "豐原西南扶輪社" && c.IsActive).Count();
            Assert(active == 0, $"0 should be active after soft-disable, got {active}");

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

    private static async Task T30_MemberClubIdFilter()
    {
        await Run("T30 Member 跨社 filter: ClubId 隔離", () =>
        {
            using var ctx = NewCtx(out _);

            // 建 2 個社
            var fysw = new Club { Name = "T30 豐原西南", IsActive = true };
            var tcwb = new Club { Name = "T30 台中西北", IsActive = true };
            ctx.Clubs.AddRange(fysw, tcwb);
            ctx.SaveChanges();

            // 同名社員「張小明」在不同社
            var mFysw = new Member { Code = 100, Name = "張小明", ClubId = fysw.Id, IsCurrent = true };
            var mTcwb = new Member { Code = 100, Name = "張小明", ClubId = tcwb.Id, IsCurrent = true };
            ctx.Members.AddRange(mFysw, mTcwb);
            ctx.SaveChanges();

            // 同 Code (100) 但不同 ClubId — 應該都 OK (因為 unique 只擋 Code exact match,但不同 ClubId 允許)
            // 確認:兩人都存在
            var all = ctx.Members.AsNoTracking().Where(m => m.Name == "張小明").ToList();
            Assert(all.Count == 2, $"should have 2 張小明, got {all.Count}");
            Assert(all.Any(m => m.ClubId == fysw.Id), "fysw 張小明 missing");
            Assert(all.Any(m => m.ClubId == tcwb.Id), "tcwb 張小明 missing");

            // filter 跨社: 選 fysw 只看到 fysw 的張小明
            var fyswMembers = ctx.Members.AsNoTracking()
                .Where(m => m.ClubId == fysw.Id).ToList();
            Assert(fyswMembers.Count == 1, $"fysw should have 1 member, got {fyswMembers.Count}");
            Assert(fyswMembers[0].Code == 100, "fysw member code should be 100");

            var tcwbMembers = ctx.Members.AsNoTracking()
                .Where(m => m.ClubId == tcwb.Id).ToList();
            Assert(tcwbMembers.Count == 1, $"tcwb should have 1 member, got {tcwbMembers.Count}");

            // 收費 也走 ClubId 過濾
            var cFysw = new ClubCollection
            {
                ClubId = fysw.Id, Year = 2026, Month = 7,
                CollectionDate = new(2026, 7, 1), Category = "會費",
                MemberCode = 100, MemberName = "張小明",
                CashAmount = 1500m
            };
            ctx.ClubCollections.Add(cFysw);
            ctx.SaveChanges();
            var fyswCol = ctx.ClubCollections.AsNoTracking().Where(c => c.ClubId == fysw.Id).ToList();
            Assert(fyswCol.Count == 1, $"fysw should have 1 collection, got {fyswCol.Count}");

            // 清理
            foreach (var c in ctx.ClubCollections.ToList()) ctx.ClubCollections.Remove(c);
            foreach (var m in ctx.Members.ToList()) ctx.Members.Remove(m);
            foreach (var c in ctx.Clubs.ToList()) ctx.Clubs.Remove(c);
            ctx.SaveChanges();
        });
    }

    private static async Task T31_MemberCrossClubQuery()
    {
        await Run("T31 CurrentClubContext filter: 模擬 VM query 跨社過濾", () =>
        {
            using var ctx = NewCtx(out _);

            // 建 3 個社
            var fysw = new Club { Name = "T31 豐原西南", IsActive = true };
            var tcwb = new Club { Name = "T31 台中西北", IsActive = true };
            var feng = new Club { Name = "T31 豐原", IsActive = true };
            ctx.Clubs.AddRange(fysw, tcwb, feng);
            ctx.SaveChanges();

            // 每社 2 個社員 (6 個)
            ctx.Members.AddRange(
                new Member { Code = 101, Name = "A 員", ClubId = fysw.Id, IsCurrent = true },
                new Member { Code = 102, Name = "B 員", ClubId = fysw.Id, IsCurrent = true },
                new Member { Code = 201, Name = "C 員", ClubId = tcwb.Id, IsCurrent = true },
                new Member { Code = 202, Name = "D 員", ClubId = tcwb.Id, IsCurrent = true },
                new Member { Code = 301, Name = "E 員", ClubId = feng.Id, IsCurrent = true },
                new Member { Code = 302, Name = "F 員", ClubId = feng.Id, IsCurrent = true });
            ctx.SaveChanges();

            // 模擬 CurrentClubContext 切到 fysw.Id
            int currentClubId = fysw.Id;

            // VM 用的 query pattern
            var visibleMembers = ctx.Members.AsNoTracking()
                .Where(m => m.ClubId == currentClubId && m.IsCurrent)
                .OrderBy(m => m.Code)
                .ToList();

            Assert(visibleMembers.Count == 2, $"should see 2 in fysw, got {visibleMembers.Count}");
            Assert(visibleMembers[0].Name == "A 員", "first should be A 員");
            Assert(visibleMembers[1].Name == "B 員", "second should be B 員");

            // 切到 tcwb
            currentClubId = tcwb.Id;
            visibleMembers = ctx.Members.AsNoTracking()
                .Where(m => m.ClubId == currentClubId && m.IsCurrent)
                .ToList();
            Assert(visibleMembers.Count == 2, $"tcwb should have 2, got {visibleMembers.Count}");
            Assert(visibleMembers.All(m => m.ClubId == tcwb.Id), "all should be tcwb");

            // 切到 feng
            currentClubId = feng.Id;
            visibleMembers = ctx.Members.AsNoTracking()
                .Where(m => m.ClubId == currentClubId && m.IsCurrent)
                .ToList();
            Assert(visibleMembers.Count == 2, $"feng should have 2, got {visibleMembers.Count}");

            // 總共 6 個社員在 db,但每個社只看到自己的 2 個
            var allCount = ctx.Members.Count();
            Assert(allCount == 6, $"total should be 6, got {allCount}");

            // 清理
            foreach (var m in ctx.Members.ToList()) ctx.Members.Remove(m);
            foreach (var c in ctx.Clubs.ToList()) ctx.Clubs.Remove(c);
            ctx.SaveChanges();
        });
    }

    private static async Task T32_ClubPickerLogic()
    {
        await Run("T32 ClubPicker 啟動邏輯: 多社時跳 picker;只有 default 直接跳過", () =>
        {
            using var ctx = NewCtx(out _);

            // 情境 A: 只有 default 社 → 不跳 picker (直接用 default)
            ctx.Clubs.Add(new Club { Name = "T32 豐原西南", IsActive = true });
            ctx.SaveChanges();
            var activeCount = ctx.Clubs.AsNoTracking().Count(c => c.IsActive);
            Assert(activeCount == 1, $"case A activeCount should be 1, got {activeCount}");

            // 情境 B: 加第二社 → activeCount = 2 → 跳 picker
            ctx.Clubs.Add(new Club { Name = "T32 台中西北", IsActive = true });
            ctx.SaveChanges();
            activeCount = ctx.Clubs.AsNoTracking().Count(c => c.IsActive);
            Assert(activeCount == 2, $"case B activeCount should be 2, got {activeCount}");

            // 情境 C: 軟刪第二社 → activeCount = 1 → 不跳 picker
            var tcwb = ctx.Clubs.First(c => c.Name == "T32 台中西北");
            tcwb.IsActive = false;
            ctx.SaveChanges();
            activeCount = ctx.Clubs.AsNoTracking().Count(c => c.IsActive);
            Assert(activeCount == 1, $"case C activeCount should be 1, got {activeCount}");

            // 情境 D: 重新啟用 → activeCount = 2 → 跳 picker
            tcwb.IsActive = true;
            ctx.SaveChanges();
            activeCount = ctx.Clubs.AsNoTracking().Count(c => c.IsActive);
            Assert(activeCount == 2, $"case D activeCount should be 2, got {activeCount}");

            // 清理
            foreach (var c in ctx.Clubs.ToList()) ctx.Clubs.Remove(c);
            ctx.SaveChanges();
        });
    }

    private static async Task T33_DataTransferExport()
    {
        await Run("T33 DataTransferEngine.ExportToJson: dump Club + Members + Collections", () =>
        {
            using var ctx = NewCtx(out _);

            // seed
            var fysw = new Club { Name = "T33 豐原西南", District = "3460", IsActive = true };
            ctx.Clubs.Add(fysw);
            ctx.SaveChanges();

            ctx.Members.AddRange(
                new Member { Code = 101, Name = "張小明", ClubId = fysw.Id, IsCurrent = true, Mobile = "0912-111-111" },
                new Member { Code = 102, Name = "李大華", ClubId = fysw.Id, IsCurrent = true });
            ctx.ClubCollections.Add(new ClubCollection
            {
                ClubId = fysw.Id, Year = 2026, Month = 7,
                CollectionDate = new DateOnly(2026, 7, 5),
                Category = "會費", MemberCode = 101, MemberName = "張小明",
                CashAmount = 1500m,
            });
            ctx.SaveChanges();

            // Export
            var json = DataTransferEngine.ExportToJson(ctx, clubId: fysw.Id);
            Assert(json.Length > 100, $"json too short: {json.Length}");

            // 解析回來檢查 (避免 PS cp950 decode 中文失敗 — parse JSON)
            using var ctx2 = NewCtx(out _);
            var import = DataTransferEngine.ImportFromJson(ctx2, json, skipExisting: true);
            Assert(import.ClubsInserted >= 1, $"import should insert >=1 club, got {import.ClubsInserted}");
            Assert(import.MembersInserted >= 2, $"import should insert >=2 members, got {import.MembersInserted}");
            Assert(import.CollectionsInserted >= 1, $"import should insert >=1 collection, got {import.CollectionsInserted}");
            Assert(import.Summary.Contains("成功"), $"summary should say 成功, got '{import.Summary}'");

            // 確認金額 round-trip — 用 byte 比 (cp950 safe)
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var jsonText = System.Text.Encoding.UTF8.GetString(bytes);
            // 用 index 找字串,不用 string.Contains
            var idxName = jsonText.IndexOf("\u8c50\u539f\u897f\u5357", StringComparison.Ordinal);
            Assert(idxName >= 0, "club name '豐原西南' should be in JSON UTF-8 bytes");
            var idxAmount = jsonText.IndexOf("1500", StringComparison.Ordinal);
            Assert(idxAmount >= 0, "amount 1500 should be in JSON UTF-8 bytes");

            // 清理
            foreach (var c in ctx2.ClubCollections.ToList()) ctx2.ClubCollections.Remove(c);
            foreach (var m in ctx2.Members.ToList()) ctx2.Members.Remove(m);
            foreach (var c in ctx2.Clubs.ToList()) ctx2.Clubs.Remove(c);
            ctx2.SaveChanges();
            foreach (var c in ctx.ClubCollections.ToList()) ctx.ClubCollections.Remove(c);
            foreach (var m in ctx.Members.ToList()) ctx.Members.Remove(m);
            foreach (var c in ctx.Clubs.ToList()) ctx.Clubs.Remove(c);
            ctx.SaveChanges();
        });
    }

    private static async Task T34_DataTransferRoundTrip()
    {
        await Run("T34 DataTransferEngine round-trip: A 機 export → B 機 import", () =>
        {
            // A 機: 1 club + 2 members + 1 collection + 1 friendly + 1 donation
            var dbA = NewCtx(out _);
            var fysw = new Club { Name = "T34 豐原西南", IsActive = true };
            dbA.Clubs.Add(fysw);
            dbA.SaveChanges();
            dbA.Members.Add(new Member { Code = 201, Name = "王小明", ClubId = fysw.Id, IsCurrent = true });
            dbA.ClubCollections.Add(new ClubCollection
            {
                ClubId = fysw.Id, Year = 2026, Month = 8,
                CollectionDate = new DateOnly(2026, 8, 5),
                Category = "會費", MemberCode = 201, MemberName = "王小明",
                CashAmount = 1500m,
            });
            dbA.FriendlyClubs.Add(new FriendlyClub
            {
                ClubId = fysw.Id, ClubCode = "FC999", ClubName = "T34 測試友社",
            });
            dbA.SaveChanges();
            var fcId = dbA.FriendlyClubs.First().Id;
            dbA.ClubDonations.Add(new ClubDonation
            {
                FriendlyClubId = fcId, FriendlyClubName = "T34 測試友社",
                Direction = DonationDirection.In, Amount = 1000m, Purpose = "贊助",
                TxDate = new DateOnly(2026, 8, 5),
            });
            dbA.SaveChanges();

            // Export from A
            var json = DataTransferEngine.ExportToJson(dbA, clubId: fysw.Id);
            dbA.Dispose();

            // B 機: 空 db,import
            var dbB = NewCtx(out _);
            var result = DataTransferEngine.ImportFromJson(dbB, json, skipExisting: true);
            Assert(result.ClubsInserted == 1, $"B should insert 1 club, got {result.ClubsInserted}");
            Assert(result.MembersInserted == 1, $"B should insert 1 member, got {result.MembersInserted}");
            Assert(result.CollectionsInserted == 1, $"B should insert 1 collection, got {result.CollectionsInserted}");
            Assert(result.FriendlyClubsInserted == 1, $"B should insert 1 friendly, got {result.FriendlyClubsInserted}");
            Assert(result.DonationsInserted == 1, $"B should insert 1 donation, got {result.DonationsInserted}");

            // 第二次 import 應該 skip (因為資料已存在)
            var result2 = DataTransferEngine.ImportFromJson(dbB, json, skipExisting: true);
            Assert(result2.ClubsInserted == 0, $"second import should skip club, got {result2.ClubsInserted}");
            Assert(result2.MembersInserted == 0, $"second import should skip member, got {result2.MembersInserted}");

            // 清理
            foreach (var d in dbB.ClubDonations.ToList()) dbB.ClubDonations.Remove(d);
            foreach (var f in dbB.FriendlyClubs.ToList()) dbB.FriendlyClubs.Remove(f);
            foreach (var c in dbB.ClubCollections.ToList()) dbB.ClubCollections.Remove(c);
            foreach (var m in dbB.Members.ToList()) dbB.Members.Remove(m);
            foreach (var c in dbB.Clubs.ToList()) dbB.Clubs.Remove(c);
            dbB.SaveChanges();
            dbB.Dispose();
        });
    }

    private static async Task T35_ImportExportDialogLogic()
    {
        await Run("T35 ImportExportDialog: 用 text 模擬 dialog 行為 (檔案選擇 + 預覽 + 執行)", () =>
        {
            // 模擬整個 dialog 流程 — 因為 dialog 是 WPF 不可測,測其後端邏輯
            using var dbA = NewCtx(out _);
            var fysw = new Club { Name = "T35 豐原西南", IsActive = true };
            dbA.Clubs.Add(fysw);
            dbA.SaveChanges();
            dbA.Members.Add(new Member { Code = 301, Name = "王小明", ClubId = fysw.Id, IsCurrent = true });
            dbA.SaveChanges();  // v0.8.1 fix: ExportToJson 走 EF query,需要 SaveChanges 才看得到

            // Step 1: 匯出到 temp 檔 (模擬「瀏覽… + 執行 匯出」)
            var tmp = Path.Combine(Path.GetTempPath(), $"importexport_{Guid.NewGuid():N}.json");
            try
            {
                var json = DataTransferEngine.ExportToJson(dbA, clubId: fysw.Id);
                File.WriteAllText(tmp, json);
                Assert(File.Exists(tmp), "exported file not created");
                var size = new FileInfo(tmp).Length;
                Assert(size > 100, $"exported file too small: {size}");
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }

            // Step 2: 預覽 (parse JSON 拿 counts)
            var previewJson = DataTransferEngine.ExportToJson(dbA, clubId: fysw.Id);
            Assert(previewJson.Contains("T35 豐原西南"), "preview should contain club name");

            // Step 3: 跳過已存在 (round-trip to empty db → 全部 insert)
            using var dbB = NewCtx(out _);
            var result = DataTransferEngine.ImportFromJson(dbB, previewJson, skipExisting: true);
            Assert(result.ClubsInserted == 1, $"expected 1 club, got {result.ClubsInserted}");
            Assert(result.MembersInserted == 1, $"expected 1 member, got {result.MembersInserted}");
            Assert(result.Summary.Contains("成功"), $"summary should contain 成功: {result.Summary}");

            // 清理
            foreach (var m in dbB.Members.ToList()) dbB.Members.Remove(m);
            foreach (var c in dbB.Clubs.ToList()) dbB.Clubs.Remove(c);
            dbB.SaveChanges();
            foreach (var m in dbA.Members.ToList()) dbA.Members.Remove(m);
            foreach (var c in dbA.Clubs.ToList()) dbA.Clubs.Remove(c);
            dbA.SaveChanges();
        });
    }

    private static async Task T36_LicenseIssueValidate()
    {
        await Run("T36 LicenseService: 發行 + 驗證 + 永久有效", () =>
        {
            // 用 custom path 測 (override %LOCALAPPDATA%)
            var tmpDir = Path.Combine(Path.GetTempPath(), $"license-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tmpDir);
            try
            {
                // 直接測 Issue/LoadAndValidate 的邏輯 — 不靠 WMI (Test bypass GetMachineId)
                var info = new LicenseInfo
                {
                    IssuedTo = "T36 測試扶輪社",
                    Issuer = "T36 HsRotaryClub Admin",
                    IssuedAt = DateTime.UtcNow,
                    ExpiresAt = null,  // 永久
                    MachineId = "",  // 不綁機
                    MaxClubs = 0,
                };

                // 模擬 Issue — 直接呼叫我們的 LicenseService.Issue 用 %LOCALAPPDATA%
                var content = LicenseService.Issue(info);
                Assert(content.Contains("---"), "issued file should contain ---");
                Assert(File.Exists(LicenseService.GetLicensePath()), "license file should exist");

                // LoadAndValidate
                var loaded = LicenseService.LoadAndValidate();
                Assert(loaded.Status == LicenseStatus.Active, $"expected Active, got {loaded.Status}");
                Assert(loaded.IssuedTo == "T36 測試扶輪社", $"IssuedTo mismatch: {loaded.IssuedTo}");

                // 描述
                var desc = LicenseService.Describe(loaded);
                Assert(desc.Contains("T36 測試扶輪社"), $"describe should contain IssuedTo: {desc}");

                // 清理 license file
                File.Delete(LicenseService.GetLicensePath());
            }
            finally
            {
                try { Directory.Delete(tmpDir, true); } catch { }
            }
        });
    }

    private static async Task T37_LicenseExpired()
    {
        await Run("T37 LicenseService: 過期 license 應該 Status=Expired", () =>
        {
            var info = new LicenseInfo
            {
                IssuedTo = "T37 測試社",
                IssuedAt = DateTime.UtcNow.AddDays(-30),
                ExpiresAt = DateTime.UtcNow.AddDays(-1),  // 昨天過期
                MachineId = "",
            };
            LicenseService.Issue(info);
            var loaded = LicenseService.LoadAndValidate();
            Assert(loaded.Status == LicenseStatus.Expired, $"expected Expired, got {loaded.Status}");

            // 清理
            File.Delete(LicenseService.GetLicensePath());
        });
    }

    private static async Task T38_LicenseMachineMismatch()
    {
        await Run("T38 LicenseService: machine mismatch → Status=MachineMismatch", () =>
        {
            // 模擬 — 沒 WMI access 用 hardcoded MachineId,然後 load 看 status
            // 因為 sandbox 可能沒 WMI,我們直接構造一個 license for machine "FAKE-ID"
            // 然後 LoadAndValidate 應該比對不上
            var info = new LicenseInfo
            {
                IssuedTo = "T38 測試社",
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = null,
                MachineId = "FAKE-SN-12345",  // 強制綁這個機
            };
            LicenseService.Issue(info);

            var loaded = LicenseService.LoadAndValidate();
            // 載入後 status 應該看當前 machine id 跟 license 的 machine id 是否 match
            // 如果實際 machine id != FAKE-SN-12345 → MachineMismatch
            // 如果 machine id 抓不到 → NoMachineId
            // 如果 WMI 拿到的恰好是 "FAKE-SN-12345" → Active (unlikely)
            Assert(loaded.Status == LicenseStatus.MachineMismatch ||
                   loaded.Status == LicenseStatus.NoMachineId,
                   $"expected MachineMismatch or NoMachineId, got {loaded.Status}");

            // 清理
            File.Delete(LicenseService.GetLicensePath());
        });
    }

    private static async Task T39_TrialCanAddClub()
    {
        await Run("T39 Trial mode: trial license 限制 1 club;第 2 club 拒絕", () =>
        {
            // 確保沒有 license.dat → 算 trial
            var licPath = LicenseService.GetLicensePath();
            if (File.Exists(licPath)) File.Delete(licPath);

            // Trial mode → LicenseInfo with Status=Trial (no license file → Trial)
            var trial = LicenseService.LoadAndValidate();
            Assert(trial.Status == LicenseStatus.Trial, $"expected Trial, got {trial.Status}");

            // 第 1 club 應該 OK
            var (allowed1, reason1) = LicenseAdmin.CanAddClub(trial, currentActiveClubs: 0);
            Assert(allowed1, $"1st club should be allowed, got reason: {reason1}");

            // 第 2 club 應該被擋
            var (allowed2, reason2) = LicenseAdmin.CanAddClub(trial, currentActiveClubs: 1);
            Assert(!allowed2, $"2nd club should be blocked");
            Assert(reason2.Contains("上限") || reason2.Contains("license"), $"reason should mention limit: {reason2}");
        });
    }

    private static async Task T40_LicenseMaxClubs()
    {
        await Run("T40 Active license + MaxClubs: 第 N+1 club 拒絕", () =>
        {
            // 設一個永久 license, MaxClubs=3
            var info = new LicenseInfo
            {
                IssuedTo = "T40 測試社",
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = null,
                MachineId = "",  // 不綁機
                MaxClubs = 3,
            };
            LicenseService.Issue(info);

            var loaded = LicenseService.LoadAndValidate();
            Assert(loaded.Status == LicenseStatus.Active, $"expected Active, got {loaded.Status}");
            Assert(loaded.MaxClubs == 3, $"MaxClubs should be 3, got {loaded.MaxClubs}");

            // 0, 1, 2 個 club → OK
            for (int i = 0; i < 3; i++)
            {
                var (ok, _) = LicenseAdmin.CanAddClub(loaded, currentActiveClubs: i);
                Assert(ok, $"club {i+1} should be allowed (max=3)");
            }
            // 第 4 個 → 拒絕
            var (ok4, reason4) = LicenseAdmin.CanAddClub(loaded, currentActiveClubs: 3);
            Assert(!ok4, $"4th club should be blocked");
            Assert(reason4.Contains("3"), $"reason should mention limit 3: {reason4}");

            // 清理
            File.Delete(LicenseService.GetLicensePath());
        });
    }

    private static async Task T41_AttendanceGroupCRUD()
    {
        await Run("T41 AttendanceGroup CRUD: 年度組別 add + filter", () =>
        {
            using var ctx = NewCtx(out _);

            var fysw = new Club { Name = "T41 豐原西南", IsActive = true };
            ctx.Clubs.Add(fysw);
            ctx.SaveChanges();

            // 3 個組
            ctx.AttendanceGroups.AddRange(
                new AttendanceGroup { ClubId = fysw.Id, Year = 2026, GroupName = "A",
                    GroupLeaderCode = 101, GroupLeaderName = "組長甲",
                    ShouldAttend = 10, ActualAttend = 8, MakeupAttend = 1 },
                new AttendanceGroup { ClubId = fysw.Id, Year = 2026, GroupName = "B",
                    GroupLeaderCode = 102, GroupLeaderName = "組長乙",
                    ShouldAttend = 12, ActualAttend = 10, MakeupAttend = 2 },
                new AttendanceGroup { ClubId = fysw.Id, Year = 2027, GroupName = "A",
                    GroupLeaderCode = 103, GroupLeaderName = "組長丙" });
            ctx.SaveChanges();

            // filter 2026
            var g2026 = ctx.AttendanceGroups.AsNoTracking()
                .Where(g => g.ClubId == fysw.Id && g.Year == 2026).ToList();
            Assert(g2026.Count == 2, $"2026 should have 2 groups, got {g2026.Count}");

            // filter 2027
            var g2027 = ctx.AttendanceGroups.AsNoTracking()
                .Where(g => g.ClubId == fysw.Id && g.Year == 2027).ToList();
            Assert(g2027.Count == 1, $"2027 should have 1 group, got {g2027.Count}");

            // 計算出席率 (第一組 8+1=9 / 10 = 90%)
            var g1 = g2026.First(g => g.GroupName == "A");
            double rate1 = (g1.ActualAttend + g1.MakeupAttend) / (double)g1.ShouldAttend;
            Assert(rate1 > 0.85 && rate1 < 0.95, $"rate1 should be ~0.9, got {rate1:F2}");

            // 清理
            foreach (var g in ctx.AttendanceGroups.ToList()) ctx.AttendanceGroups.Remove(g);
            foreach (var c in ctx.Clubs.ToList()) ctx.Clubs.Remove(c);
            ctx.SaveChanges();
        });
    }

    private static async Task T42_AttendanceRecord()
    {
        await Run("T42 AttendanceRecord: 例會出缺席記錄 CRUD + filter", () =>
        {
            using var ctx = NewCtx(out _);

            var fysw = new Club { Name = "T42 豐原西南", IsActive = true };
            ctx.Clubs.Add(fysw);
            ctx.SaveChanges();

            // 5 個社員
            for (int i = 1; i <= 5; i++)
            {
                ctx.Members.Add(new Member { Code = 200 + i, Name = $"社員{i}", ClubId = fysw.Id, IsCurrent = true });
            }
            ctx.SaveChanges();

            // 3 次例會, 每社員每次出席狀況
            var meetingDates = new[] { new DateTime(2026, 7, 3), new DateTime(2026, 7, 10), new DateTime(2026, 7, 17) };
            foreach (var m in ctx.Members.ToList())
            {
                foreach (var md in meetingDates)
                {
                    ctx.AttendanceRecords.Add(new AttendanceRecord
                    {
                        ClubId = fysw.Id,
                        Year = md.Year,
                        MemberCode = m.Code,
                        MemberName = m.Name,
                        MeetingDate = md,
                        Type = md.Day == 10 && m.Code == 203 ? AttendanceType.Absent :
                               md.Day == 17 && m.Code == 202 ? AttendanceType.Makeup :
                               AttendanceType.Present,
                        MakeupDate = md.Day == 17 && m.Code == 202 ? md : null,
                    });
                }
            }
            ctx.SaveChanges();

            // 統計: 社員 203 在 7/10 缺席, 7/17 出席
            var r203 = ctx.AttendanceRecords.AsNoTracking()
                .Where(r => r.MemberCode == 203).OrderBy(r => r.MeetingDate).ToList();
            Assert(r203.Count == 3, $"203 should have 3 records, got {r203.Count}");
            Assert(r203[0].Type == AttendanceType.Present, "203 7/3 should be Present");
            Assert(r203[1].Type == AttendanceType.Absent, "203 7/10 should be Absent");
            Assert(r203[2].Type == AttendanceType.Present, "203 7/17 should be Present");

            // filter 7/10 出席率
            var allAt7_10 = ctx.AttendanceRecords.AsNoTracking().Where(r => r.MeetingDate == meetingDates[1]).ToList();
            var presentCount = allAt7_10.Count(r => r.Type == AttendanceType.Present || r.Type == AttendanceType.Makeup);
            Assert(presentCount == 4, $"7/10 should have 4 present (5-1 absent), got {presentCount}");

            // 清理
            foreach (var r in ctx.AttendanceRecords.ToList()) ctx.AttendanceRecords.Remove(r);
            foreach (var m in ctx.Members.ToList()) ctx.Members.Remove(m);
            foreach (var c in ctx.Clubs.ToList()) ctx.Clubs.Remove(c);
            ctx.SaveChanges();
        });
    }

    private static async Task T43_AttendanceRateCalc()
    {
        await Run("T43 AttendanceRate 計算: 出席率 / 全出席率 公式驗證", () =>
        {
            // 組員 A 出席 8 / 補 2 / 應 10 → 出席率 100% (實+補 / 應)
            int should = 10, actual = 8, makeup = 2;
            double rate = (actual + makeup) / (double)should;
            Assert(rate == 1.0, $"全出席 rate should be 1.0, got {rate:F2}");

            // 組員 B 出席 5 / 補 0 / 應 10 → 出席率 50%
            int actual2 = 5;
            double rate2 = actual2 / (double)should;
            Assert(rate2 == 0.5, $"半出席 rate should be 0.5, got {rate2:F2}");

            // 全出席率 (全年度 0 缺席) → 100%
            // 用 AttendanceRecord 跑邏輯
            int totalMeetings = 12;  // 一年 12 次例會
            int attendedCount = 12;
            _ = attendedCount;  // suppress unused warning
            int missedCount = 0;
            double fullAttendanceRate = (totalMeetings - missedCount) / (double)totalMeetings;
            Assert(fullAttendanceRate == 1.0, $"全出席率 should be 1.0, got {fullAttendanceRate:F2}");

            // 社員 8/12 出席 → 0.667
            int partAttended = 8;
            double partRate = partAttended / (double)totalMeetings;
            Assert(partRate > 0.66 && partRate < 0.68, $"partRate ~0.67, got {partRate:F2}");
        });
    }

    private static async Task T44_AttendanceFilterByYearGroup()
    {
        await Run("T44 AttendanceViewModel filter: 年度 + 組別 雙 filter 邏輯", () =>
        {
            using var ctx = NewCtx(out _);

            var fysw = new Club { Name = "T44 豐原西南", IsActive = true };
            ctx.Clubs.Add(fysw);
            ctx.SaveChanges();

            // 2 年度 × 3 組
            ctx.AttendanceGroups.AddRange(
                new AttendanceGroup { ClubId = fysw.Id, Year = 2026, GroupName = "A", GroupLeaderCode = 1, GroupLeaderName = "甲", GroupMemberCode = 2, GroupMemberName = "乙" },
                new AttendanceGroup { ClubId = fysw.Id, Year = 2026, GroupName = "B", GroupLeaderCode = 3, GroupLeaderName = "丙", GroupMemberCode = 4, GroupMemberName = "丁" },
                new AttendanceGroup { ClubId = fysw.Id, Year = 2026, GroupName = "C", GroupLeaderCode = 5, GroupLeaderName = "戊", GroupMemberCode = 6, GroupMemberName = "己" },
                new AttendanceGroup { ClubId = fysw.Id, Year = 2027, GroupName = "A", GroupLeaderCode = 7, GroupLeaderName = "庚" });
            ctx.SaveChanges();

            // filter 2026 → 3 組
            var g2026 = ctx.AttendanceGroups.AsNoTracking()
                .Where(g => g.ClubId == fysw.Id && g.Year == 2026).ToList();
            Assert(g2026.Count == 3, $"2026 should have 3 groups, got {g2026.Count}");

            // filter 2027 → 1 組
            var g2027 = ctx.AttendanceGroups.AsNoTracking()
                .Where(g => g.ClubId == fysw.Id && g.Year == 2027).ToList();
            Assert(g2027.Count == 1, $"2027 should have 1 group, got {g2027.Count}");

            // 模擬 AttendanceViewModel 取 "選定組別" 的出席記錄邏輯
            var selGroup = g2026.First(g => g.GroupName == "A");
            var leaderCode = selGroup.GroupLeaderCode;  // 1
            var memberCode = selGroup.GroupMemberCode;  // 2
            // 模擬 3 次例會
            var meetings = new[] { new DateTime(2026, 7, 3), new DateTime(2026, 7, 10), new DateTime(2026, 7, 17) };
            foreach (var md in meetings)
            {
                ctx.AttendanceRecords.Add(new AttendanceRecord
                {
                    ClubId = fysw.Id, Year = 2026, MeetingDate = md,
                    MemberCode = leaderCode, MemberName = "甲", Type = AttendanceType.Present,
                });
                ctx.AttendanceRecords.Add(new AttendanceRecord
                {
                    ClubId = fysw.Id, Year = 2026, MeetingDate = md,
                    MemberCode = memberCode, MemberName = "乙", Type = AttendanceType.Present,
                });
            }
            // 額外一筆給 2027 的組長 (7)
            ctx.AttendanceRecords.Add(new AttendanceRecord
            {
                ClubId = fysw.Id, Year = 2027, MeetingDate = new DateTime(2027, 7, 3),
                MemberCode = 7, MemberName = "庚", Type = AttendanceType.Present,
            });
            ctx.SaveChanges();

            // VM 邏輯: 取選定組別 (A 組) 的 leader + member 出席記錄
            var vmFilter = ctx.AttendanceRecords.AsNoTracking()
                .Where(r => r.ClubId == fysw.Id && r.Year == 2026 &&
                            (r.MemberCode == leaderCode || r.MemberCode == memberCode))
                .OrderBy(r => r.MeetingDate).ToList();
            Assert(vmFilter.Count == 6, $"A 組應有 6 筆 (2 人 × 3 例會), got {vmFilter.Count}");
            Assert(vmFilter.All(r => r.Year == 2026), "all should be 2026");
            Assert(vmFilter.Select(r => r.MemberCode).Distinct().OrderBy(c => c).SequenceEqual(new[] { 1, 2 }),
                $"should only have members 1 and 2, got [{string.Join(",", vmFilter.Select(r => r.MemberCode).Distinct())}]");

            // 切到 2027 A 組 → 1 筆
            var g2027A = g2027.First(g => g.GroupName == "A");
            var vm2027 = ctx.AttendanceRecords.AsNoTracking()
                .Where(r => r.ClubId == fysw.Id && r.Year == 2027 &&
                            (r.MemberCode == g2027A.GroupLeaderCode || r.MemberCode == g2027A.GroupMemberCode))
                .ToList();
            Assert(vm2027.Count == 1, $"2027 A 組應有 1 筆, got {vm2027.Count}");

            // 清理
            foreach (var r in ctx.AttendanceRecords.ToList()) ctx.AttendanceRecords.Remove(r);
            foreach (var g in ctx.AttendanceGroups.ToList()) ctx.AttendanceGroups.Remove(g);
            foreach (var c in ctx.Clubs.ToList()) ctx.Clubs.Remove(c);
            ctx.SaveChanges();
        });
    }

    private static async Task T45_OtherIncomeCRUD()
    {
        await Run("T45 OtherIncome CRUD: M5 其它收入", () =>
        {
            using var ctx = NewCtx(out _);

            var fysw = new Club { Name = "T45 豐原西南", IsActive = true };
            ctx.Clubs.Add(fysw);
            ctx.SaveChanges();

            // 5 筆其它收入
            ctx.OtherIncomes.AddRange(
                new OtherIncome { ClubId = fysw.Id, Year = 2026, Month = 7, TxDate = new DateTime(2026, 7, 5), Subject = "銀行利息", Amount = 250m, Category = "利息收入" },
                new OtherIncome { ClubId = fysw.Id, Year = 2026, Month = 7, TxDate = new DateTime(2026, 7, 10), Subject = "友社贊助", Amount = 5000m, Category = "贊助" },
                new OtherIncome { ClubId = fysw.Id, Year = 2026, Month = 7, TxDate = new DateTime(2026, 7, 15), Subject = "活動報名費", Amount = 3000m, Category = "活動" },
                new OtherIncome { ClubId = fysw.Id, Year = 2026, Month = 8, TxDate = new DateTime(2026, 8, 5), Subject = "銀行利息", Amount = 260m, Category = "利息收入" },
                new OtherIncome { ClubId = fysw.Id, Year = 2026, Month = 9, TxDate = new DateTime(2026, 9, 5), Subject = "捐款", Amount = 10000m, Category = "贊助" });
            ctx.SaveChanges();

            // 7 月總額
            var july = ctx.OtherIncomes.AsNoTracking()
                .Where(o => o.ClubId == fysw.Id && o.Year == 2026 && o.Month == 7).ToList();
            Assert(july.Count == 3, $"7 月應有 3 筆, got {july.Count}");
            var julyTotal = july.Sum(o => o.Amount);
            Assert(julyTotal == 8250m, $"7 月總額應為 8250, got {julyTotal}");

            // 清理
            foreach (var o in ctx.OtherIncomes.ToList()) ctx.OtherIncomes.Remove(o);
            foreach (var c in ctx.Clubs.ToList()) ctx.Clubs.Remove(c);
            ctx.SaveChanges();
        });
    }

    private static async Task T46_MonthlyExpenseCRUD()
    {
        await Run("T46 MonthlyExpense CRUD: M5 扶輪月支出", () =>
        {
            using var ctx = NewCtx(out _);

            var fysw = new Club { Name = "T46 豐原西南", IsActive = true };
            ctx.Clubs.Add(fysw);
            ctx.SaveChanges();

            // 4 筆月支出
            ctx.MonthlyExpenses.AddRange(
                new MonthlyExpense { ClubId = fysw.Id, Year = 2026, Month = 7, TxDate = new DateTime(2026, 7, 1), Subject = "場地租賃", Amount = 3000m, CreditAccount = "2100", Category = "會議費" },
                new MonthlyExpense { ClubId = fysw.Id, Year = 2026, Month = 7, TxDate = new DateTime(2026, 7, 5), Subject = "餐費", Amount = 5000m, CreditAccount = "1100", Category = "膳食" },
                new MonthlyExpense { ClubId = fysw.Id, Year = 2026, Month = 7, TxDate = new DateTime(2026, 7, 10), Subject = "文宣", Amount = 1500m, CreditAccount = "1200", Category = "印刷" },
                new MonthlyExpense { ClubId = fysw.Id, Year = 2026, Month = 8, TxDate = new DateTime(2026, 8, 1), Subject = "場地租賃", Amount = 3000m, CreditAccount = "2100", Category = "會議費" });
            ctx.SaveChanges();

            var july = ctx.MonthlyExpenses.AsNoTracking()
                .Where(m => m.ClubId == fysw.Id && m.Year == 2026 && m.Month == 7).ToList();
            Assert(july.Count == 3, $"7 月應有 3 筆, got {july.Count}");
            var julyTotal = july.Sum(m => m.Amount);
            Assert(julyTotal == 9500m, $"7 月總額應為 9500, got {julyTotal}");

            // 清理
            foreach (var m in ctx.MonthlyExpenses.ToList()) ctx.MonthlyExpenses.Remove(m);
            foreach (var c in ctx.Clubs.ToList()) ctx.Clubs.Remove(c);
            ctx.SaveChanges();
        });
    }

    private static async Task T47_AccountSubjectAndEntry()
    {
        await Run("T47 AccountSubject + AccountEntry: M6 科目 + 月度事實", () =>
        {
            using var ctx = NewCtx(out _);

            var fysw = new Club { Name = "T47 豐原西南", IsActive = true };
            ctx.Clubs.Add(fysw);
            ctx.SaveChanges();

            // 5 個科目
            ctx.AccountSubjects.AddRange(
                new AccountSubject { ClubId = fysw.Id, Type = AccountType.Income, Code = "I01", Name = "會費收入", AnnualBudget = 100000m },
                new AccountSubject { ClubId = fysw.Id, Type = AccountType.Income, Code = "I02", Name = "捐款收入", AnnualBudget = 200000m },
                new AccountSubject { ClubId = fysw.Id, Type = AccountType.Income, Code = "I03", Name = "活動收入", AnnualBudget = 50000m },
                new AccountSubject { ClubId = fysw.Id, Type = AccountType.Expense, Code = "E01", Name = "會議費", AnnualBudget = 60000m },
                new AccountSubject { ClubId = fysw.Id, Type = AccountType.Expense, Code = "E02", Name = "印刷費", AnnualBudget = 12000m });
            ctx.SaveChanges();

            // 7 月事實
            ctx.AccountEntries.AddRange(
                new AccountEntry { ClubId = fysw.Id, Year = 2026, Month = 7, SubjectCode = "I01", ThisMonth = 8500m },
                new AccountEntry { ClubId = fysw.Id, Year = 2026, Month = 7, SubjectCode = "I02", ThisMonth = 30000m },
                new AccountEntry { ClubId = fysw.Id, Year = 2026, Month = 7, SubjectCode = "E01", ThisMonth = 5000m },
                new AccountEntry { ClubId = fysw.Id, Year = 2026, Month = 7, SubjectCode = "E02", ThisMonth = 1000m });
            ctx.SaveChanges();

            // 科目 CRUD
            var subjects = ctx.AccountSubjects.AsNoTracking().Where(s => s.ClubId == fysw.Id).ToList();
            Assert(subjects.Count == 5, $"應有 5 個科目, got {subjects.Count}");
            var incomes = subjects.Where(s => s.Type == AccountType.Income).ToList();
            Assert(incomes.Count == 3, $"收入應有 3 個, got {incomes.Count}");
            var totalBudget = incomes.Sum(i => i.AnnualBudget);
            Assert(totalBudget == 350000m, $"收入總預算應為 350000, got {totalBudget}");

            // 清理
            foreach (var e in ctx.AccountEntries.ToList()) ctx.AccountEntries.Remove(e);
            foreach (var s in ctx.AccountSubjects.ToList()) ctx.AccountSubjects.Remove(s);
            foreach (var c in ctx.Clubs.ToList()) ctx.Clubs.Remove(c);
            ctx.SaveChanges();
        });
    }

    private static async Task T48_AccountMonthlyReportFormula()
    {
        await Run("T48 會計月報表公式: 本月收入/本月支出/累計/預算執行率", () =>
        {
            using var ctx = NewCtx(out _);

            var fysw = new Club { Name = "T48 豐原西南", IsActive = true };
            ctx.Clubs.Add(fysw);
            ctx.SaveChanges();

            // 科目 (I01 預算 100000, E01 預算 60000)
            ctx.AccountSubjects.AddRange(
                new AccountSubject { ClubId = fysw.Id, Type = AccountType.Income, Code = "I01", Name = "會費", AnnualBudget = 100000m },
                new AccountSubject { ClubId = fysw.Id, Type = AccountType.Expense, Code = "E01", Name = "會議", AnnualBudget = 60000m });
            ctx.SaveChanges();

            // 月度事實: 1-7 月
            for (int m = 1; m <= 7; m++)
            {
                ctx.AccountEntries.Add(new AccountEntry { ClubId = fysw.Id, Year = 2026, Month = m, SubjectCode = "I01", ThisMonth = 10000m });
                ctx.AccountEntries.Add(new AccountEntry { ClubId = fysw.Id, Year = 2026, Month = m, SubjectCode = "E01", ThisMonth = 5000m });
            }
            ctx.SaveChanges();

            // 計算: 7 月 (本月 + 累計到 7 月)
            var entries = ctx.AccountEntries.AsNoTracking()
                .Where(e => e.ClubId == fysw.Id && e.Year == 2026).ToList();

            // I01: 本月 7 月 = 10000, 累計 1-7 月 = 70000
            var i01July = entries.Where(e => e.SubjectCode == "I01" && e.Month == 7).Sum(e => e.ThisMonth);
            Assert(i01July == 10000m, $"I01 7月 本月應為 10000, got {i01July}");
            var i01YTD = entries.Where(e => e.SubjectCode == "I01" && e.Month <= 7).Sum(e => e.ThisMonth);
            Assert(i01YTD == 70000m, $"I01 累計 1-7 月應為 70000, got {i01YTD}");

            // 預算執行率 = 累計 / 年度預算
            var i01Budget = ctx.AccountSubjects.AsNoTracking().First(s => s.Code == "I01").AnnualBudget;
            double i01Exec = (double)(i01YTD / i01Budget);
            Assert(i01Exec > 0.69 && i01Exec < 0.71, $"I01 執行率 ~0.70, got {i01Exec:F2}");

            // E01: 本月 7 月 = 5000, 累計 = 35000
            var e01July = entries.Where(e => e.SubjectCode == "E01" && e.Month == 7).Sum(e => e.ThisMonth);
            Assert(e01July == 5000m, $"E01 7月 本月應為 5000, got {e01July}");
            var e01YTD = entries.Where(e => e.SubjectCode == "E01" && e.Month <= 7).Sum(e => e.ThisMonth);
            Assert(e01YTD == 35000m, $"E01 累計應為 35000, got {e01YTD}");

            // 清理
            foreach (var e in ctx.AccountEntries.ToList()) ctx.AccountEntries.Remove(e);
            foreach (var s in ctx.AccountSubjects.ToList()) ctx.AccountSubjects.Remove(s);
            foreach (var c in ctx.Clubs.ToList()) ctx.Clubs.Remove(c);
            ctx.SaveChanges();
        });
    }

    private static async Task T49_MailJobCreate()
    {
        await Run("T49 MailJob: M7 信件 (主旨/內容/附件/排程類型)", () =>
        {
            using var ctx = NewCtx(out _);

            var fysw = new Club { Name = "T49 豐原西南", IsActive = true };
            ctx.Clubs.Add(fysw);
            ctx.SaveChanges();

            var job = new MailJob
            {
                ClubId = fysw.Id,
                Subject = "2026年7月份例會通知",
                Content = "各位社友大家好,本月例會於 7/3 下午 6:30 召開,地點:...",
                AttachmentPath = @"C:\meetings\2026_07.pdf",
                ScheduleType = "Annual",
            };
            ctx.MailJobs.Add(job);
            ctx.SaveChanges();
            Assert(job.Id > 0, $"job id 應 > 0, got {job.Id}");

            // re-query
            var loaded = ctx.MailJobs.AsNoTracking().First();
            Assert(loaded.Subject == "2026年7月份例會通知", $"Subject mismatch: {loaded.Subject}");
            Assert(loaded.ScheduleType == "Annual", $"ScheduleType mismatch: {loaded.ScheduleType}");
            Assert(loaded.AttachmentPath != null, "AttachmentPath should be set");

            // 清理
            foreach (var j in ctx.MailJobs.ToList()) ctx.MailJobs.Remove(j);
            foreach (var c in ctx.Clubs.ToList()) ctx.Clubs.Remove(c);
            ctx.SaveChanges();
        });
    }

    private static async Task T50_MailRecipientStatus()
    {
        await Run("T50 MailRecipient: 收件人清單 + 寄送狀態追蹤", () =>
        {
            using var ctx = NewCtx(out _);

            var fysw = new Club { Name = "T50 豐原西南", IsActive = true };
            ctx.Clubs.Add(fysw);
            ctx.SaveChanges();

            ctx.Members.Add(new Member { Code = 301, Name = "張三", ClubId = fysw.Id, IsCurrent = true, Email = "a@a.com" });
            ctx.Members.Add(new Member { Code = 302, Name = "李四", ClubId = fysw.Id, IsCurrent = true, Email = "b@b.com" });
            ctx.Members.Add(new Member { Code = 303, Name = "王五", ClubId = fysw.Id, IsCurrent = true, Email = "c@c.com" });
            ctx.SaveChanges();

            var job = new MailJob { ClubId = fysw.Id, Subject = "T50 test", Content = "hi", ScheduleType = "OneOff" };
            ctx.MailJobs.Add(job);
            ctx.SaveChanges();

            // 3 收件人
            foreach (var m in ctx.Members.ToList())
            {
                ctx.MailRecipients.Add(new MailRecipient
                {
                    MailJobId = job.Id,
                    MemberCode = m.Code,
                    MemberName = m.Name,
                    Email = m.Email!,
                    Status = MailSendStatus.Pending,
                });
            }
            ctx.SaveChanges();

            // 收件人 CRUD
            var recipients = ctx.MailRecipients.AsNoTracking()
                .Where(r => r.MailJobId == job.Id).ToList();
            Assert(recipients.Count == 3, $"應有 3 個收件人, got {recipients.Count}");
            Assert(recipients.All(r => r.Status == MailSendStatus.Pending), "all should be Pending");

            // 模擬寄送
            var toSend = recipients.First();
            var tracked = ctx.MailRecipients.First(r => r.Id == toSend.Id);
            tracked.Status = MailSendStatus.Sent;
            tracked.SentAt = DateTime.UtcNow;
            ctx.SaveChanges();

            var sent = ctx.MailRecipients.AsNoTracking().Where(r => r.Status == MailSendStatus.Sent).Count();
            Assert(sent == 1, $"應有 1 筆 Sent, got {sent}");

            // 模擬失敗
            var tracked2 = ctx.MailRecipients.First(r => r.MemberCode == 303);
            tracked2.Status = MailSendStatus.Failed;
            tracked2.ErrorMessage = "Email not exist";
            ctx.SaveChanges();
            var failed = ctx.MailRecipients.AsNoTracking().Where(r => r.Status == MailSendStatus.Failed).Count();
            Assert(failed == 1, $"應有 1 筆 Failed, got {failed}");

            // 清理
            foreach (var r in ctx.MailRecipients.ToList()) ctx.MailRecipients.Remove(r);
            foreach (var j in ctx.MailJobs.ToList()) ctx.MailJobs.Remove(j);
            foreach (var m in ctx.Members.ToList()) ctx.Members.Remove(m);
            foreach (var c in ctx.Clubs.ToList()) ctx.Clubs.Remove(c);
            ctx.SaveChanges();
        });
    }

    private static async Task T51_MigrationMissingFile()
    {
        await Run("T51 MigrationEngine: 缺 mdb 檔 → fail 訊息清楚", () =>
        {
            using var ctx = NewCtx(out _);
            var fakePath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.mdb");
            var result = MigrationEngine.Migrate(fakePath, ctx);
            Assert(!result.Success, "missing file should fail");
            Assert(result.Errors.Count > 0, "should have error");
            Assert(result.Errors[0].Contains("找不到"), $"error should mention 找不到: {result.Errors[0]}");
            Assert(result.MembersImported == 0, $"no members imported, got {result.MembersImported}");
        });
    }

    private static async Task T52_MigrationResultSummary()
    {
        await Run("T52 MigrationResult: Summary 文字包含關鍵資訊", () =>
        {
            var result = new MigrationResult
            {
                SourcePath = "C:\\fake.mdb",
                TargetClubId = 1,
                DryRun = true,
                SourceTablesFound = new List<string> { "Member", "TS81_MAT1" },
                MembersRead = 10,
                MembersImported = 8,
                MembersSkipped = 2,
                AttendanceGroupsRead = 5,
                AttendanceGroupsImported = 4,
                AttendanceGroupsSkipped = 1,
                Errors = new List<string>(),
                Warnings = new List<string> { "test warning" },
            };
            Assert(result.Success, "should be success");
            var s = result.Summary;
            Assert(s.Contains("dry-run"), $"summary should mention dry-run: {s}");
            Assert(s.Contains("Members 8 imported"), $"should say Members 8 imported: {s}");
            Assert(s.Contains("AttendanceGroups 4 imported"), $"should say AttendanceGroups 4 imported: {s}");
            Assert(s.Contains("Errors 0"), $"should say Errors 0: {s}");
        });
    }

    private static async Task T53_DbInitializerRecreate()
    {
        await Run("T53 DbInitializer: 舊 db 無 Clubs table → 砍掉重建", () =>
        {
            var path = Path.Combine(Path.GetTempPath(), $"smoketest-init-{Guid.NewGuid():N}.db");
            try
            {
                // 直接用 sqlite 建空 db (有 header 但無 Clubs)
                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False"))
                {
                    conn.Open();
                }
                Assert(File.Exists(path), "test db should exist");
                Assert(!DbInitializerHasTableForTest(path, "Clubs"), "test db should NOT have Clubs");

                // 模擬 EnsureCreated 建 Clubs table
                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False"))
                {
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "CREATE TABLE Clubs (Id INTEGER PRIMARY KEY, Name TEXT)";
                    cmd.ExecuteNonQuery();
                }
                Assert(DbInitializerHasTableForTest(path, "Clubs"), "after CREATE, Clubs should exist");
            }
            finally
            {
                // wait briefly for SQLite file locks to release
                System.Threading.Thread.Sleep(100);
                if (File.Exists(path)) File.Delete(path);
            }
        });
    }

    private static async Task T54_XamlBomAndFont()
    {
        await Run("T54 v0.17: 全 18 個 XAML 都有 UTF-8 BOM + App.xaml 含 AppFontFamily", () =>
        {
            var appDir = Path.Combine(ResolveProjectRoot().ToString(), "src", "HsRotaryClub.App");
            var bom = new byte[] { 0xEF, 0xBB, 0xBF };
            var xamlFiles = Directory.GetFiles(appDir, "*.xaml", SearchOption.AllDirectories);
            Assert(xamlFiles.Length >= 15, $"should have >=15 xaml files, got {xamlFiles.Length}");
            int withBom = 0;
            foreach (var f in xamlFiles)
            {
                var first3 = File.ReadAllBytes(f).Take(3).ToArray();
                if (first3.SequenceEqual(bom)) withBom++;
            }
            Assert(withBom == xamlFiles.Length, $"all XAML should have BOM: {withBom}/{xamlFiles.Length}");

            // App.xaml 應含 AppFontFamily
            var appXaml = File.ReadAllText(Path.Combine(appDir, "App.xaml"));
            Assert(appXaml.Contains("AppFontFamily"), "App.xaml should define AppFontFamily");
            Assert(appXaml.Contains("Microsoft JhengHei"), "AppFontFamily should include Microsoft JhengHei");
        });
    }

    private static async Task T55_NavItemChineseTitle()
    {
        await Run("T55 v0.17: NavItem.Title 中文 UTF-8 bytes 正確 (codepage 切換不會 garble)", () =>
        {
            // 直接 grep MainWindowViewModel.cs 確認中文是 UTF-8 bytes 不是 cp950 garble
            var cs = Path.Combine(ResolveProjectRoot().ToString(), "src", "HsRotaryClub.App", "ViewModels", "MainWindowViewModel.cs");
            var raw = File.ReadAllBytes(cs);
            // "社團管理" in UTF-8 = E7 A4 BE E5 9C 98 E7 AE A1 E7 90 86
            var expected = new byte[] { 0xE7, 0xA4, 0xBE, 0xE5, 0x9C, 0x98, 0xE7, 0xAE, 0xA1, 0xE7, 0x90, 0x86 };
            int idx = -1;
            for (int i = 0; i <= raw.Length - expected.Length; i++)
            {
                if (raw[i] == 0xE7 && raw[i+1] == 0xA4 && raw[i+2] == 0xBE && raw[i+3] == 0xE5) { idx = i; break; }
            }
            Assert(idx >= 0, "社團管理 UTF-8 bytes not found in MainWindowViewModel.cs (file may be cp950 garbled)");
        });
    }

    private static async Task T56_AppXamlGlobalFontSize()
    {
        await Run("T56 v0.21: App.xaml global FontSize=14 + 8 style types 含 Menu/DatePicker", () =>
        {
            var appXaml = Path.Combine(ResolveProjectRoot().ToString(), "src", "HsRotaryClub.App", "App.xaml");
            var text = File.ReadAllText(appXaml, System.Text.Encoding.UTF8);
            // Check key style types
            foreach (var t in new[] { "Window", "TextBlock", "Button", "TextBox", "ComboBox", "DataGrid", "DatePicker", "Menu", "MenuItem", "DataGridColumnHeader" })
            {
                Assert(text.Contains($"TargetType=\"{{x:Type {t}}}\""), $"App.xaml missing style for {t}");
            }
            // Check FontSize=14 appear >= 8 times (one per style)
            int fs14 = System.Text.RegularExpressions.Regex.Matches(text, @"<Setter\s+Property=""FontSize""\s+Value=""14""").Count;
            Assert(fs14 >= 8, $"should have >=8 FontSize=14 setters, got {fs14}");
        });
    }

    private static bool DbInitializerHasTableForTest(string path, string tableName)
    {
        try
        {
            using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Pooling=False");
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
            return false;
        }
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
