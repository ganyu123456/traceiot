using System.Text.Json;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using TraceIot.Devices;
using TraceIot.Realtime;
using Volo.Abp.Application.Services;

namespace TraceIot.Application.Realtime;

public class RealtimeAppService : ApplicationService, IRealtimeAppService
{
    private readonly IDeviceRepository _deviceRepo;
    private readonly IConnectionMultiplexer _redis;

    public RealtimeAppService(IDeviceRepository deviceRepo, IConnectionMultiplexer redis)
    {
        _deviceRepo = deviceRepo;
        _redis = redis;
    }

    public async Task<List<DeviceRealtimeDto>> GetAllPositionsAsync()
    {
        var db = _redis.GetDatabase();
        var devices = await _deviceRepo.GetListAsync(d => d.IsEnabled);
        var result = new List<DeviceRealtimeDto>();

        foreach (var device in devices)
        {
            var key = $"{TraceIotConsts.RedisRealKeyPrefix}{device.DeviceCode}";
            var json = await db.StringGetAsync(key);

            var dto = new DeviceRealtimeDto
            {
                DeviceId   = device.Id.ToString(),
                DeviceCode = device.DeviceCode,
                DeviceName = device.DeviceName,
                Online     = false
            };

            if (json.HasValue)
            {
                var cache = JsonSerializer.Deserialize<RealPositionCache>(json!);
                if (cache != null)
                {
                    dto.Lat       = cache.Lat;
                    dto.Lng       = cache.Lng;
                    dto.Speed     = cache.Speed;
                    dto.Direction = cache.Direction;
                    dto.Timestamp = cache.Timestamp;
                    dto.Online    = cache.Online;
                }
            }

            result.Add(dto);
        }

        return result;
    }

    public async Task<DeviceRealtimeDto?> GetPositionAsync(string deviceCode)
    {
        var device = await _deviceRepo.FindByCodeAsync(deviceCode);
        if (device == null) return null;

        var db = _redis.GetDatabase();
        var key = $"{TraceIotConsts.RedisRealKeyPrefix}{deviceCode}";
        var json = await db.StringGetAsync(key);

        var dto = new DeviceRealtimeDto
        {
            DeviceId   = device.Id.ToString(),
            DeviceCode = device.DeviceCode,
            DeviceName = device.DeviceName,
            Online     = false
        };

        if (json.HasValue)
        {
            var cache = JsonSerializer.Deserialize<RealPositionCache>(json!);
            if (cache != null)
            {
                dto.Lat       = cache.Lat;
                dto.Lng       = cache.Lng;
                dto.Speed     = cache.Speed;
                dto.Direction = cache.Direction;
                dto.Timestamp = cache.Timestamp;
                dto.Online    = cache.Online;
            }
        }

        return dto;
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync()
    {
        var devices = await _deviceRepo.GetListAsync();
        var db = _redis.GetDatabase();

        var onlineCount   = 0;
        var offlineCount  = 0;
        var disabledCount = 0;

        foreach (var device in devices)
        {
            if (!device.IsEnabled)
            {
                disabledCount++;
                continue;
            }

            var key = $"{TraceIotConsts.RedisRealKeyPrefix}{device.DeviceCode}";
            var json = await db.StringGetAsync(key);
            if (json.HasValue)
            {
                var cache = JsonSerializer.Deserialize<RealPositionCache>(json!);
                if (cache?.Online == true)
                    onlineCount++;
                else
                    offlineCount++;
            }
            else
            {
                offlineCount++;
            }
        }

        return new DashboardStatsDto
        {
            TotalDevices    = devices.Count,
            OnlineDevices   = onlineCount,
            OfflineDevices  = offlineCount,
            DisabledDevices = disabledCount
        };
    }
}
