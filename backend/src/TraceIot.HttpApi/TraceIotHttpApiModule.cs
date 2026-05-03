using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Modularity;

namespace TraceIot;

[DependsOn(
    typeof(AbpAspNetCoreMvcModule),
    typeof(TraceIotApplicationContractsModule)
)]
public class TraceIotHttpApiModule : AbpModule
{
}
