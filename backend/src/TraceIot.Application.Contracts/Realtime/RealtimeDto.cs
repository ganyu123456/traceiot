namespace TraceIot.Realtime;

/// <summary>设备实时位置信息（来自 Redis）</summary>
public class DeviceRealtimeDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double Speed { get; set; }
    public double Direction { get; set; }
    public long Timestamp { get; set; }
    public bool Online { get; set; }
    public string? GroupName { get; set; }
}

/// <summary>Redis 存储的实时位置 JSON 格式</summary>
public class RealPositionCache
{
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double Speed { get; set; }
    public double Direction { get; set; }
    public long Timestamp { get; set; }
    public bool Online { get; set; }
}
