using System.ComponentModel.DataAnnotations;
using TraceIot.Enums;
using Volo.Abp.Application.Dtos;

namespace TraceIot.Devices;

public class DeviceDto : FullAuditedEntityDto<Guid>
{
    public string DeviceCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    public DeviceStatus Status { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? LastHeartbeatAt { get; set; }
    public decimal? LastLat { get; set; }
    public decimal? LastLng { get; set; }
    public decimal? LastSpeed { get; set; }
    public decimal? LastDirection { get; set; }
    public string? Remark { get; set; }
    public decimal OverspeedThreshold { get; set; }
    public int OfflineTimeoutSec { get; set; }
    public bool GeofenceEnabled { get; set; }
}

public class CreateDeviceDto
{
    [Required, MaxLength(64)]
    public string DeviceCode { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string DeviceName { get; set; } = string.Empty;

    public Guid? GroupId { get; set; }

    [MaxLength(512)]
    public string? Remark { get; set; }

    public decimal OverspeedThreshold { get; set; } = 120;
    public int OfflineTimeoutSec { get; set; } = 60;
}

public class UpdateDeviceDto
{
    [Required, MaxLength(128)]
    public string DeviceName { get; set; } = string.Empty;

    public Guid? GroupId { get; set; }

    [MaxLength(512)]
    public string? Remark { get; set; }

    public decimal OverspeedThreshold { get; set; } = 120;
    public int OfflineTimeoutSec { get; set; } = 60;
    public bool GeofenceEnabled { get; set; }
    public string? GeofenceConfig { get; set; }
}

public class DevicePagedRequestDto : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public Guid? GroupId { get; set; }
    public DeviceStatus? Status { get; set; }
    public bool? IsEnabled { get; set; }
}
