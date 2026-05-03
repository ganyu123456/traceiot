using TraceIot.Enums;
using Volo.Abp.Domain.Entities.Auditing;

namespace TraceIot.Devices;

/// <summary>GPS 设备实体</summary>
public class Device : FullAuditedAggregateRoot<Guid>
{
    /// <summary>设备编号（IMEI 或自定义唯一码）</summary>
    public string DeviceCode { get; set; } = string.Empty;

    /// <summary>设备名称</summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>所属分组</summary>
    public Guid? GroupId { get; set; }

    /// <summary>在线状态</summary>
    public DeviceStatus Status { get; set; } = DeviceStatus.Offline;

    /// <summary>是否启用</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>最后心跳时间</summary>
    public DateTime? LastHeartbeatAt { get; set; }

    /// <summary>最后已知纬度</summary>
    public decimal? LastLat { get; set; }

    /// <summary>最后已知经度</summary>
    public decimal? LastLng { get; set; }

    /// <summary>最后已知速度</summary>
    public decimal? LastSpeed { get; set; }

    /// <summary>最后已知方向（度）</summary>
    public decimal? LastDirection { get; set; }

    /// <summary>备注</summary>
    public string? Remark { get; set; }

    // 预留告警配置字段
    public decimal OverspeedThreshold { get; set; } = 120;
    public int OfflineTimeoutSec { get; set; } = 60;
    public bool GeofenceEnabled { get; set; } = false;
    public string? GeofenceConfig { get; set; }

    protected Device() { }

    public Device(Guid id, string deviceCode, string deviceName, Guid? groupId = null)
        : base(id)
    {
        DeviceCode = deviceCode;
        DeviceName = deviceName;
        GroupId = groupId;
    }

    public void UpdateHeartbeat(decimal lat, decimal lng, decimal speed, decimal direction)
    {
        LastHeartbeatAt = DateTime.UtcNow;
        LastLat = lat;
        LastLng = lng;
        LastSpeed = speed;
        LastDirection = direction;
        Status = DeviceStatus.Online;
    }

    public void SetOffline()
    {
        Status = DeviceStatus.Offline;
    }
}
