using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TraceIot.EntityFrameworkCore;

/// <summary>
/// 设计时 DbContext 工厂，供 dotnet ef migrations 命令使用。
/// 不依赖完整应用启动（Redis、MQTT 等），仅需 PostgreSQL 连接串。
/// </summary>
public class TraceIotDbContextFactory : IDesignTimeDbContextFactory<TraceIotDbContext>
{
    public TraceIotDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=traceiot;Username=postgres;Password=traceiot123";

        var builder = new DbContextOptionsBuilder<TraceIotDbContext>()
            .UseNpgsql(connectionString);

        return new TraceIotDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var basePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "../TraceIot.HttpApi.Host"
        );

        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }
}
