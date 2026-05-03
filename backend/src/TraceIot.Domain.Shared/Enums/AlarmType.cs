namespace TraceIot.Enums;

/// <summary>告警类型</summary>
public enum AlarmType : short
{
    /// <summary>超速</summary>
    Overspeed = 1,
    /// <summary>电子围栏</summary>
    Geofence = 2,
    /// <summary>设备离线</summary>
    Offline = 3
}
