using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ams.Infrastructure.Persistence;

/// <summary>
/// Used only by the <c>dotnet ef</c> tooling. Having this means migration commands do not need
/// the API's configuration (or a JWT signing key) to be present, so an evaluator can run
/// <c>dotnet ef migrations add</c> / <c>database update</c> without any environment setup.
/// <para>
/// Override the target database with the <c>AMS_CONNECTION_STRING</c> environment variable.
/// </para>
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string FallbackConnectionString =
        "Host=localhost;Port=5432;Database=ams;Username=ams;Password=ams_dev_password";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("AMS_CONNECTION_STRING") ?? FallbackConnectionString;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
