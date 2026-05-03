using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace TraceIot.MqttWorker;

[DependsOn(typeof(TraceIotDomainModule))]
public class TraceIotMqttWorkerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHostedService<GpsMqttWorker>();
        context.Services.AddHostedService<DeviceOfflineChecker>();
        context.Services.AddSingleton<GpsMessageHandler>();
    }
}
