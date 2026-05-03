using System.Text.Json.Serialization;

namespace TraceIot.MqttWorker;

/// <summary>设备上报的 MQTT JSON Payload</summary>
public class GpsLocationPayload
{
    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lng")]
    public double Lng { get; set; }

    [JsonPropertyName("speed")]
    public double Speed { get; set; }

    [JsonPropertyName("direction")]
    public double Direction { get; set; }

    /// <summary>Unix 毫秒时间戳</summary>
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}
