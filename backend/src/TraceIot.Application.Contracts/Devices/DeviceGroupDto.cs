using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace TraceIot.Devices;

public class DeviceGroupDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public int DeviceCount { get; set; }
}

public class CreateUpdateDeviceGroupDto
{
    [Required, MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }
}

public class DeviceGroupPagedRequestDto : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
}
