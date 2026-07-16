using HsRotaryClub.Domain;
using Microsoft.EntityFrameworkCore;

namespace HsRotaryClub.Infrastructure;

public class RotaryDbContext : DbContext
{
    public RotaryDbContext(DbContextOptions<RotaryDbContext> options) : base(options) { }

    public DbSet<Member> Members => Set<Member>();
    public DbSet<ClubCollection> ClubCollections => Set<ClubCollection>();
    public DbSet<MonthlyReceivableSpec> MonthlyReceivableSpecs => Set<MonthlyReceivableSpec>();
    public DbSet<FriendlyClub> FriendlyClubs => Set<FriendlyClub>();
    public DbSet<ClubDonation> ClubDonations => Set<ClubDonation>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Member>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(50).IsRequired();
            e.Property(x => x.EnglishName).HasMaxLength(100);
            e.Property(x => x.IdNumber).HasMaxLength(20);
            e.Property(x => x.Code).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.Name);
        });

        mb.Entity<ClubCollection>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Category).HasMaxLength(50).IsRequired();
            e.Property(x => x.CashAmount).HasDefaultValue(0m);
            e.Property(x => x.CheckAmount).HasDefaultValue(0m);
            e.HasIndex(x => new { x.Year, x.Month });
            e.HasIndex(x => x.MemberCode);
        });

        mb.Entity<MonthlyReceivableSpec>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Item).HasMaxLength(50).IsRequired();
            e.Property(x => x.Amount).HasDefaultValue(0m);
            e.Property(x => x.SettledAmount).HasDefaultValue(0m);
            e.HasIndex(x => new { x.Year, x.Month, x.MemberCode });
        });

        mb.Entity<FriendlyClub>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ClubCode).HasMaxLength(20).IsRequired();
            e.Property(x => x.ClubName).HasMaxLength(80).IsRequired();
            e.HasIndex(x => x.ClubCode).IsUnique();
        });

        mb.Entity<ClubDonation>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasDefaultValue(0m);
            e.HasIndex(x => x.FriendlyClubId);
            e.HasOne<FriendlyClub>()
                .WithMany()
                .HasForeignKey(x => x.FriendlyClubId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
