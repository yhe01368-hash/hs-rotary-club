using HsRotaryClub.Domain;
using Microsoft.EntityFrameworkCore;

namespace HsRotaryClub.Infrastructure;

public class RotaryDbContext : DbContext
{
    public RotaryDbContext() { }
    public RotaryDbContext(DbContextOptions<RotaryDbContext> options) : base(options) { }

    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<ClubCollection> ClubCollections => Set<ClubCollection>();
    public DbSet<MonthlyReceivableSpec> MonthlyReceivableSpecs => Set<MonthlyReceivableSpec>();
    public DbSet<FriendlyClub> FriendlyClubs => Set<FriendlyClub>();
    public DbSet<ClubDonation> ClubDonations => Set<ClubDonation>();
    public DbSet<AttendanceGroup> AttendanceGroups => Set<AttendanceGroup>();  // v0.10
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();  // v0.10
    public DbSet<OtherIncome> OtherIncomes => Set<OtherIncome>();  // v0.12
    public DbSet<MonthlyExpense> MonthlyExpenses => Set<MonthlyExpense>();  // v0.12
    public DbSet<AccountSubject> AccountSubjects => Set<AccountSubject>();  // v0.12
    public DbSet<AccountEntry> AccountEntries => Set<AccountEntry>();  // v0.12
    public DbSet<MailJob> MailJobs => Set<MailJob>();  // v0.12
    public DbSet<MailRecipient> MailRecipients => Set<MailRecipient>();  // v0.12
    public DbSet<User> Users => Set<User>();  // v0.38 登入帳號

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Club>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(80).IsRequired();
            e.Property(x => x.District).HasMaxLength(20);
            e.Property(x => x.Contact).HasMaxLength(50);
            e.Property(x => x.ContactEmail).HasMaxLength(100);
            e.Property(x => x.Remarks).HasMaxLength(500);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            // v0.32: 拿掉 Clubs.Name 的 unique — 不同社團可有同名 (e.g. 多個示範/分區).
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.IsActive);
        });

        mb.Entity<Member>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(50).IsRequired();
            e.Property(x => x.EnglishName).HasMaxLength(100);
            e.Property(x => x.IdNumber).HasMaxLength(20);
            e.Property(x => x.Code).IsRequired();
            e.Property(x => x.IsCurrent).HasDefaultValue(true);
            e.Property(x => x.ClubId).HasDefaultValue(ClubDefaults.DefaultClubId);
            e.HasIndex(x => new { x.ClubId, x.Code }).IsUnique();
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.IsCurrent);
            e.HasIndex(x => x.ClubId);  // v0.7 A2: 多社 filter
        });

        mb.Entity<ClubCollection>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Category).HasMaxLength(50).IsRequired();
            e.Property(x => x.CashAmount).HasDefaultValue(0m);
            e.Property(x => x.CheckAmount).HasDefaultValue(0m);
            e.Property(x => x.ClubId).HasDefaultValue(ClubDefaults.DefaultClubId);
            e.HasIndex(x => new { x.Year, x.Month });
            e.HasIndex(x => x.MemberCode);
            e.HasIndex(x => x.ClubId);  // v0.7 A2
        });

        mb.Entity<MonthlyReceivableSpec>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Item).HasMaxLength(50).IsRequired();
            e.Property(x => x.Amount).HasDefaultValue(0m);
            e.Property(x => x.SettledAmount).HasDefaultValue(0m);
            e.Property(x => x.ClubId).HasDefaultValue(ClubDefaults.DefaultClubId);
            e.HasIndex(x => new { x.Year, x.Month, x.MemberCode });
            e.HasIndex(x => x.ClubId);  // v0.7 A2
        });

        mb.Entity<FriendlyClub>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ClubCode).HasMaxLength(20).IsRequired();
            e.Property(x => x.ClubName).HasMaxLength(80).IsRequired();
            e.Property(x => x.ClubId).HasDefaultValue(ClubDefaults.DefaultClubId);
            e.HasIndex(x => x.ClubCode).IsUnique();
            e.HasIndex(x => x.ClubId);  // v0.7 A2
        });

        mb.Entity<ClubDonation>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasDefaultValue(0m);
            e.Property(x => x.Purpose).HasMaxLength(100);
            e.HasIndex(x => x.FriendlyClubId);
            e.HasIndex(x => x.TxDate);
            e.HasOne<FriendlyClub>()
                .WithMany()
                .HasForeignKey(x => x.FriendlyClubId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // v0.10 — 出席模組
        mb.Entity<AttendanceGroup>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.GroupName).HasMaxLength(20).IsRequired();
            e.Property(x => x.GroupLeaderName).HasMaxLength(50);
            e.Property(x => x.GroupMemberName).HasMaxLength(50);
            e.Property(x => x.ClubId).HasDefaultValue(ClubDefaults.DefaultClubId);
            e.HasIndex(x => new { x.ClubId, x.Year, x.GroupName });
        });
        mb.Entity<AttendanceRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.MemberName).HasMaxLength(50);
            e.Property(x => x.ClubId).HasDefaultValue(ClubDefaults.DefaultClubId);
            e.HasIndex(x => new { x.ClubId, x.Year, x.MemberCode });
            e.HasIndex(x => x.MeetingDate);
        });

        // v0.12 — M5 其它收支
        mb.Entity<OtherIncome>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Subject).HasMaxLength(50);
            e.Property(x => x.Description).HasMaxLength(200);
            e.Property(x => x.Category).HasMaxLength(30);
            e.Property(x => x.VoucherNo).HasMaxLength(20);
            e.Property(x => x.ClubId).HasDefaultValue(ClubDefaults.DefaultClubId);
            e.HasIndex(x => new { x.ClubId, x.Year, x.Month });
        });
        mb.Entity<MonthlyExpense>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Subject).HasMaxLength(50);
            e.Property(x => x.Description).HasMaxLength(200);
            e.Property(x => x.CreditAccount).HasMaxLength(20);
            e.Property(x => x.Category).HasMaxLength(30);
            e.Property(x => x.VoucherNo).HasMaxLength(20);
            e.Property(x => x.ClubId).HasDefaultValue(ClubDefaults.DefaultClubId);
            e.HasIndex(x => new { x.ClubId, x.Year, x.Month });
        });

        // v0.12 — M6 會計
        mb.Entity<AccountSubject>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.Property(x => x.Name).HasMaxLength(50).IsRequired();
            e.Property(x => x.ClubId).HasDefaultValue(ClubDefaults.DefaultClubId);
            e.HasIndex(x => new { x.ClubId, x.Type, x.Code }).IsUnique();
        });
        mb.Entity<AccountEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.SubjectCode).HasMaxLength(20);
            e.Property(x => x.ClubId).HasDefaultValue(ClubDefaults.DefaultClubId);
            e.HasIndex(x => new { x.ClubId, x.Year, x.Month, x.SubjectCode }).IsUnique();
        });

        // v0.12 — M7 信件
        mb.Entity<MailJob>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Subject).HasMaxLength(200).IsRequired();
            e.Property(x => x.AttachmentPath).HasMaxLength(500);
            e.Property(x => x.ScheduleType).HasMaxLength(20);
            e.Property(x => x.ClubId).HasDefaultValue(ClubDefaults.DefaultClubId);
            e.HasIndex(x => x.ClubId);
        });
        mb.Entity<MailRecipient>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.MemberName).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.ErrorMessage).HasMaxLength(500);
            e.HasIndex(x => x.MailJobId);
        });

        mb.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Username).HasMaxLength(50).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(100);
            e.Property(x => x.Role).HasConversion<int>();
            e.HasIndex(x => x.Username).IsUnique();
        });
    }
}