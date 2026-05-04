using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Application;
using Volo.Abp.AutoMapper;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;

namespace TraceIot;

[DependsOn(
    typeof(AbpDddApplicationModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpIdentityApplicationModule),
    typeof(TraceIotApplicationContractsModule),
    typeof(TraceIotDomainModule)
)]
public class TraceIotApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAutoMapperObjectMapper<TraceIotApplicationModule>();

        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<TraceIotApplicationModule>(validate: false);
        });
    }
}
