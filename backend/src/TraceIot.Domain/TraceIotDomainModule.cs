using Volo.Abp.Domain;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;

namespace TraceIot;

[DependsOn(
    typeof(AbpDddDomainModule),
    typeof(AbpIdentityDomainModule),
    typeof(TraceIotDomainSharedModule)
)]
public class TraceIotDomainModule : AbpModule
{
}
