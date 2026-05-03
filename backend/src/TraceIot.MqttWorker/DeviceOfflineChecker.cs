using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TraceIot.Realtime;

namespace TraceIot.MqttWorker;

/// <summary>
/// 定时检测设备心跳，TTL 过期时将设备标记为离线
/// </summary>
public class DeviceOfflineChecker : BackgroundService
{
    private readonly IConnectionMultiplexer          _redis;
    private readonly ILogger<DeviceOfflineChecker>   _logger;

    public DeviceOfflineChecker(IConnectionMultiplexer redis, ILogger<DeviceOfflineChecker> logger)
    {
        _redis  = redis;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("设备离线检测器已启动，检测间隔 {Interval} 秒",
            TraceIotConsts.OfflineCheckIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckOfflineDevicesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "离线检测异常");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(TraceIotConsts.OfflineCheckIntervalSeconds),
                stoppingToken);
        }
    }

    private async Task CheckOfflineDevicesAsync()
    {
        var db = _redis.GetDatabase();

        // 获取在线集合中所有 deviceId
        var onlineMembers = await db.SetMembersAsync(TraceIotConsts.RedisOnlineSetKey);
        var offlineCount  = 0;

        foreach (var member in onlineMembers)
        {
            var deviceId     = member.ToString();
            var heartbeatKey = $"{TraceIotConsts.RedisHeartbeatKeyPrefix}{deviceId}";
            var heartbeatTtl = await db.KeyTimeToLiveAsync(heartbeatKey);

            // 心跳 key 已过期或不存在 → 设备离线
            if (heartbeatTtl == null || heartbeatTtl.Value.TotalSeconds <= 0)
            {
                var realKey = $"{TraceIotConsts.RedisRealKeyPrefix}{deviceId}";
                var json    = await db.StringGetAsync(realKey);

                if (json.HasValue)
                {
                    var cache = JsonSerializer.Deserialize<RealPositionCache>(json!);
                    if (cache != null && cache.Online)
                    {
                        cache.Online = false;
                        await db.StringSetAsync(realKey, JsonSerializer.Serialize(cache));
                        _logger.LogInformation("设备 {DeviceId} 已离线（心跳超时）", deviceId);
                        offlineCount++;
                    }
                }

                // 从在线集合中移除
                await db.SetRemoveAsync(TraceIotConsts.RedisOnlineSetKey, deviceId);
            }
        }

        if (offlineCount > 0)
            _logger.LogInformation("本轮检测标记 {Count} 台设备离线", offlineCount);
    }
}
