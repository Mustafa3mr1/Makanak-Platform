using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Makanak.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MakanakDbContext>
{
    public MakanakDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("MAKANAK_CONNECTION")
                 ?? "Server=.;Database=MakanakDb;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<MakanakDbContext>()
            .UseSqlServer(cs, sql => sql.MigrationsAssembly(typeof(MakanakDbContext).Assembly.FullName))
            .Options;
        return new MakanakDbContext(options);
    }
}
