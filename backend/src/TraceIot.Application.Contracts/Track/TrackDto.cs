using System.ComponentModel.DataAnnotations;

namespace TraceIot.Track;

/// <summary>历史轨迹查询请求</summary>
public class TrackQueryDto
{
    [Required]
    public string DeviceCode { get; set; } = string.Empty;

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    /// <summary>最大返回点数，防止数据量过大（默认5000）</summary>
    public int MaxPoints { get; set; } = 5000;
}

/// <summary>单个轨迹点</summary>
public class TrackPointDto
{
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double Speed { get; set; }
    public double Direction { get; set; }
    public DateTime Time { get; set; }
}

/// <summary>轨迹查询结果</summary>
public class TrackResultDto
{
    public string DeviceCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public int TotalPoints { get; set; }
    public List<TrackPointDto> Points { get; set; } = new();
}
