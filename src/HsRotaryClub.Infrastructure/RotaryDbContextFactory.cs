using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HsRotaryClub.Infrastructure;

/// <summary>
/// 給 EF Core tooling (dotnet ef migrations / dotnet ef database update) 用。
/// AppDbContext 跑時拿 DbContextOptions from DI 而不是這條路徑。
/// </summary>
public class RotaryDbContextFactory : IDesignTimeDbContextFactory<RotaryDbContext>
{
    public RotaryDbContext CreateDbContext(string[] args)
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HsRotaryClub",
            "rotary.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var builder = new DbContextOptionsBuilder<RotaryDbContext>();
        builder.UseSqlite($"Data Source={dbPath}");
        return new RotaryDbContext(builder.Options);
    }
}
