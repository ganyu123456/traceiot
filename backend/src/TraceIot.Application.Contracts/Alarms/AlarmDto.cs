using System.ComponentModel.DataAnnotations;
using TraceIot.Enums;
using Volo.Abp.Application.Dtos;

namespace TraceIot.Alarms;

public class AlarmRecordDto : EntityDto<Guid>
{
    public Guid DeviceId { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public AlarmType AlarmType { get; set; }
    public string AlarmTypeName => AlarmType switch
    {
        AlarmType.Overspeed => "超速",
        AlarmType.Geofence  => "电子围栏",
        AlarmType.Offline   => "设备离线",
        _                   => "未知"
    };
    public decimal? AlarmValue { get; set; }
    public decimal? Lat { get; set; }
    public decimal? Lng { get; set; }
    public DateTime TriggeredAt { get; set; }
    public DateTime? HandledAt { get; set; }
    public bool IsHandled { get; set; }
    public string? HandlerNote { get; set; }
}

public class AlarmPagedRequestDto : PagedAndSortedResultRequestDto
{
    public Guid? DeviceId { get; set; }
    public AlarmType? AlarmType { get; set; }
    public bool? IsHandled { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}

public class HandleAlarmDto
{
    [MaxLength(512)]
    public string? Note { get; set; }
}
