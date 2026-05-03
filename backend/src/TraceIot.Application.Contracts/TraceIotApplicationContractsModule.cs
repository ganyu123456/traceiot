using Volo.Abp.Application;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;

namespace TraceIot;

[DependsOn(
    typeof(AbpDddApplicationContractsModule),
    typeof(AbpIdentityApplicationContractsModule),
    typeof(TraceIotDomainSharedModule)
)]
public class TraceIotApplicationContractsModule : AbpModule
{
}
