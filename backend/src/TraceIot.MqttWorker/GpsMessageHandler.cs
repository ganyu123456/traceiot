using System.Text.Json;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TraceIot.Devices;
using TraceIot.Realtime;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace TraceIot.MqttWorker;

/// <summary>
/// 处理收到的 GPS MQTT 消息：解析 → 写 Redis → 写 InfluxDB → 同步 PostgreSQL 设备状态
/// 若收到未知 deviceId 则自动注册设备
/// </summary>
public class GpsMessageHandler
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IInfluxDBClient        _influx;
    private readonly IServiceScopeFactory   _scopeFactory;
    private readonly ILogger<GpsMessageHandler> _logger;
    private readonly string _org;
    private readonly string _bucket;

    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public GpsMessageHandler(
        IConnectionMultiplexer redis,
        IInfluxDBClient influx,
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<GpsMessageHandler> logger)
    {
        _redis        = redis;
        _influx       = influx;
        _scopeFactory = scopeFactory;
        _logger       = logger;
        _org          = config["InfluxDB:Org"]    ?? "traceiot";
        _bucket       = config["InfluxDB:Bucket"] ?? "gps";
    }

    public async Task HandleLocationAsync(string topic, string payload)
    {
        try
        {
            var data = JsonSerializer.Deserialize<GpsLocationPayload>(payload, _jsonOpts);
            if (data == null || string.IsNullOrWhiteSpace(data.DeviceId))
            {
                _logger.LogWarning("收到无效 GPS Payload，Topic: {Topic}, Payload: {Payload}", topic, payload);
                return;
            }

            // timestamp=0 时使用服务器当前时间（STM32 无网络时钟同步时的兜底）
            if (data.Timestamp <= 0)
                data.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            _logger.LogInformation("设备 {DeviceId} 位置更新: lat={Lat}, lng={Lng}", data.DeviceId, data.Lat, data.Lng);

            await Task.WhenAll(
                WriteRedisAsync(data),
                WriteInfluxAsync(data)
            );

            // 异步同步 PostgreSQL（不阻塞主流程）
            _ = Task.Run(() => SyncDeviceAsync(data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理 GPS 消息异常，Topic: {Topic}", topic);
        }
    }

    public async Task HandleHeartbeatAsync(string deviceId)
    {
        try
        {
            var db = _redis.GetDatabase();
            var heartbeatKey = $"{TraceIotConsts.RedisHeartbeatKeyPrefix}{deviceId}";
            await db.StringSetAsync(heartbeatKey, "1",
                TimeSpan.FromSeconds(TraceIotConsts.DeviceOfflineTimeoutSeconds));
            _logger.LogDebug("设备 {DeviceId} 心跳续期", deviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理心跳异常，DeviceId: {DeviceId}", deviceId);
        }
    }

    private async Task WriteRedisAsync(GpsLocationPayload data)
    {
        var db = _redis.GetDatabase();

        var cache = new RealPositionCache
        {
            Lat       = data.Lat,
            Lng       = data.Lng,
            Speed     = data.Speed,
            Direction = data.Direction,
            Timestamp = data.Timestamp,
            Online    = true
        };

        var realKey      = $"{TraceIotConsts.RedisRealKeyPrefix}{data.DeviceId}";
        var heartbeatKey = $"{TraceIotConsts.RedisHeartbeatKeyPrefix}{data.DeviceId}";

        var json = JsonSerializer.Serialize(cache);

        await db.StringSetAsync(realKey, json);
        await db.StringSetAsync(heartbeatKey, "1",
            TimeSpan.FromSeconds(TraceIotConsts.DeviceOfflineTimeoutSeconds));
        await db.SetAddAsync(TraceIotConsts.RedisOnlineSetKey, data.DeviceId);
    }

    private async Task WriteInfluxAsync(GpsLocationPayload data)
    {
        try
        {
            var writeApi = _influx.GetWriteApiAsync();
            var ts = DateTimeOffset.FromUnixTimeMilliseconds(data.Timestamp).UtcDateTime;

            var point = PointData
                .Measurement(TraceIotConsts.InfluxMeasurement)
                .Tag("device_id", data.DeviceId)
                .Field("lat",       data.Lat)
                .Field("lng",       data.Lng)
                .Field("speed",     data.Speed)
                .Field("direction", data.Direction)
                .Timestamp(ts, WritePrecision.Ms);

            await writeApi.WritePointAsync(point, _bucket, _org);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "写入 InfluxDB 失败（设备 {DeviceId}），实时功能不受影响", data.DeviceId);
        }
    }

    /// <summary>
    /// 同步设备状态到 PostgreSQL：
    /// - 若设备不存在则自动注册（以 deviceId 作为 DeviceCode 和 DeviceName）
    /// - 更新最新位置和在线状态
    /// </summary>
    private async Task SyncDeviceAsync(GpsLocationPayload data)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWorkManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
            var deviceRepo        = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();

            using var uow = unitOfWorkManager.Begin(requiresNew: true);

            var device = await deviceRepo.FindByCodeAsync(data.DeviceId);
            if (device == null)
            {
                _logger.LogInformation("自动注册新设备: {DeviceId}", data.DeviceId);
                device = new Device(Guid.NewGuid(), data.DeviceId, $"设备-{data.DeviceId}");
                await deviceRepo.InsertAsync(device);
            }

            device.UpdateHeartbeat(
                (decimal)data.Lat,
                (decimal)data.Lng,
                (decimal)data.Speed,
                (decimal)data.Direction);

            await deviceRepo.UpdateAsync(device);
            await uow.CompleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "同步设备到 PostgreSQL 失败（DeviceId: {DeviceId}），不影响实时功能", data.DeviceId);
        }
    }
}
