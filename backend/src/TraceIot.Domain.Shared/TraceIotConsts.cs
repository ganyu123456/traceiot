namespace TraceIot;

/// <summary>全局常量</summary>
public static class TraceIotConsts
{
    /// <summary>MQTT Topic 前缀</summary>
    public const string MqttTopicPrefix = "gps";

    /// <summary>MQTT 设备位置上报 Topic 模板（{0} 为 deviceCode）</summary>
    public const string MqttLocationTopicTemplate = "gps/{0}/location";

    /// <summary>MQTT 心跳 Topic 模板</summary>
    public const string MqttHeartbeatTopicTemplate = "gps/{0}/heartbeat";

    /// <summary>Redis key 前缀 - 实时位置</summary>
    public const string RedisRealKeyPrefix = "gps:real:";

    /// <summary>Redis key - 在线设备集合</summary>
    public const string RedisOnlineSetKey = "gps:online:set";

    /// <summary>Redis key 前缀 - 心跳（带 TTL）</summary>
    public const string RedisHeartbeatKeyPrefix = "gps:heartbeat:";

    /// <summary>设备离线判定秒数（心跳 TTL）</summary>
    public const int DeviceOfflineTimeoutSeconds = 60;

    /// <summary>离线检查器扫描间隔（秒）</summary>
    public const int OfflineCheckIntervalSeconds = 30;

    /// <summary>InfluxDB measurement 名称</summary>
    public const string InfluxMeasurement = "gps_track";
}
