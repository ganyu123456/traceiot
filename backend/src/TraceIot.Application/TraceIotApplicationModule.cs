using Volo.Abp.Application;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;

namespace TraceIot;

[DependsOn(
    typeof(AbpDddApplicationModule),
    typeof(AbpIdentityApplicationModule),
    typeof(TraceIotApplicationContractsModule),
    typeof(TraceIotDomainModule)
)]
public class TraceIotApplicationModule : AbpModule
{
}
