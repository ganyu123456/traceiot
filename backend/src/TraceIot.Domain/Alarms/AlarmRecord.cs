using TraceIot.Enums;
using Volo.Abp.Domain.Entities;

namespace TraceIot.Alarms;

/// <summary>告警记录</summary>
public class AlarmRecord : AggregateRoot<Guid>
{
    public Guid DeviceId { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public AlarmType AlarmType { get; set; }

    /// <summary>触发告警时的值（超速：km/h；离线：0）</summary>
    public decimal? AlarmValue { get; set; }
    public decimal? Lat { get; set; }
    public decimal? Lng { get; set; }
    public DateTime TriggeredAt { get; set; }
    public DateTime? HandledAt { get; set; }
    public bool IsHandled { get; set; }
    public Guid? HandlerId { get; set; }
    public string? HandlerNote { get; set; }

    protected AlarmRecord() { }

    public AlarmRecord(Guid id, Guid deviceId, string deviceCode, AlarmType alarmType,
        decimal? alarmValue = null, decimal? lat = null, decimal? lng = null)
        : base(id)
    {
        DeviceId = deviceId;
        DeviceCode = deviceCode;
        AlarmType = alarmType;
        AlarmValue = alarmValue;
        Lat = lat;
        Lng = lng;
        TriggeredAt = DateTime.UtcNow;
        IsHandled = false;
    }

    public void Handle(Guid handlerId, string? note)
    {
        IsHandled = true;
        HandledAt = DateTime.UtcNow;
        HandlerId = handlerId;
        HandlerNote = note;
    }
}
