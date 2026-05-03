using Volo.Abp.Application.Services;

namespace TraceIot.Realtime;

public interface IRealtimeAppService : IApplicationService
{
    /// <summary>获取所有在线设备的实时位置</summary>
    Task<List<DeviceRealtimeDto>> GetAllPositionsAsync();

    /// <summary>获取单台设备的实时位置</summary>
    Task<DeviceRealtimeDto?> GetPositionAsync(string deviceCode);

    /// <summary>获取在线设备数量统计</summary>
    Task<DashboardStatsDto> GetDashboardStatsAsync();
}

public class DashboardStatsDto
{
    public int TotalDevices { get; set; }
    public int OnlineDevices { get; set; }
    public int OfflineDevices { get; set; }
    public int DisabledDevices { get; set; }
    public int TodayAlarmCount { get; set; }
}
