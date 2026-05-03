using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using TraceIot;
using TraceIot.EntityFrameworkCore;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("TraceIoT 服务正在启动...");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host
        .UseSerilog()
        .UseAutofac();

    await builder.AddApplicationAsync<TraceIotHttpApiHostModule>();

    var app = builder.Build();

    // 自动执行 EF Core Migration
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<TraceIotDbContext>();
        await db.Database.MigrateAsync();
        Log.Information("数据库迁移完成");

        // 种子数据：创建 admin 用户
        var dataSeeder = scope.ServiceProvider.GetRequiredService<Volo.Abp.Data.IDataSeeder>();
        await dataSeeder.SeedAsync(new Volo.Abp.Data.DataSeedContext());
        Log.Information("数据初始化完成");
    }

    await app.InitializeApplicationAsync();
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "服务启动失败");
}
finally
{
    Log.CloseAndFlush();
}
