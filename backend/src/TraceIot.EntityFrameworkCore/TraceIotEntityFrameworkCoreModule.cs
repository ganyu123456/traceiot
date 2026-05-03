using Microsoft.Extensions.DependencyInjection;
using TraceIot.Alarms;
using TraceIot.Devices;
using TraceIot.EntityFrameworkCore.Repositories;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.PostgreSql;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;

namespace TraceIot.EntityFrameworkCore;

[DependsOn(
    typeof(TraceIotDomainModule),
    typeof(AbpEntityFrameworkCorePostgreSqlModule),
    typeof(AbpIdentityEntityFrameworkCoreModule),
    typeof(AbpPermissionManagementEntityFrameworkCoreModule),
    typeof(AbpSettingManagementEntityFrameworkCoreModule)
)]
public class TraceIotEntityFrameworkCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<TraceIotDbContext>(options =>
        {
            options.AddDefaultRepositories(includeAllEntities: true);
            options.AddRepository<Device, DeviceRepository>();
            options.AddRepository<DeviceGroup, DeviceGroupRepository>();
            options.AddRepository<AlarmRecord, AlarmRepository>();
        });

        Configure<AbpDbContextOptions>(options =>
        {
            options.UseNpgsql<TraceIotDbContext>();
        });
    }
}
