using System.Text.Json;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TraceIot.Realtime;

namespace TraceIot.MqttWorker;

/// <summary>
/// 处理收到的 GPS MQTT 消息：解析 → 写 Redis → 写 InfluxDB
/// </summary>
public class GpsMessageHandler
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IInfluxDBClient        _influx;
    private readonly ILogger<GpsMessageHandler> _logger;
    private readonly string _org;
    private readonly string _bucket;

    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public GpsMessageHandler(
        IConnectionMultiplexer redis,
        IInfluxDBClient influx,
        IConfiguration config,
        ILogger<GpsMessageHandler> logger)
    {
        _redis  = redis;
        _influx = influx;
        _logger = logger;
        _org    = config["InfluxDB:Org"]    ?? "traceiot";
        _bucket = config["InfluxDB:Bucket"] ?? "gps";
    }

    public async Task HandleLocationAsync(string topic, string payload)
    {
        try
        {
            var data = JsonSerializer.Deserialize<GpsLocationPayload>(payload, _jsonOpts);
            if (data == null || string.IsNullOrWhiteSpace(data.DeviceId))
            {
                _logger.LogWarning("收到无效 GPS Payload，Topic: {Topic}", topic);
                return;
            }

            _logger.LogDebug("设备 {DeviceId} 位置更新: lat={Lat}, lng={Lng}, speed={Speed}",
                data.DeviceId, data.Lat, data.Lng, data.Speed);

            await Task.WhenAll(
                WriteRedisAsync(data),
                WriteInfluxAsync(data)
            );
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
            _logger.LogDebug("设备 {DeviceId} 心跳续约", deviceId);
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

        // 加入在线集合
        await db.SetAddAsync(TraceIotConsts.RedisOnlineSetKey, data.DeviceId);
    }

    private async Task WriteInfluxAsync(GpsLocationPayload data)
    {
        var writeApi = _influx.GetWriteApiAsync();

        var point = PointData
            .Measurement(TraceIotConsts.InfluxMeasurement)
            .Tag("device_id", data.DeviceId)
            .Field("lat",       data.Lat)
            .Field("lng",       data.Lng)
            .Field("speed",     data.Speed)
            .Field("direction", data.Direction)
            .Timestamp(DateTimeOffset.FromUnixTimeMilliseconds(data.Timestamp).UtcDateTime,
                       WritePrecision.Ms);

        await writeApi.WritePointAsync(point, _bucket, _org);
    }
}
