using HsRotaryClub.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HsRotaryClub.App.Infrastructure;

/// <summary>
/// App startup 跑一次:EnsureCreated + SeedIfEmpty。
/// 之後改成 migrate-only (先生產了 dev.db 然後跑 migrations)。
/// </summary>
public sealed class DbInitializer
{
    private readonly DbContextOptions<RotaryDbContext> _options;

    public DbInitializer(DbContextOptions<RotaryDbContext> options)
    {
        _options = options;
    }

    public void Initialize()
    {
        using var db = new RotaryDbContext(_options);
        db.Database.EnsureCreated();
        SeedData.SeedIfEmpty(db);
    }
}
