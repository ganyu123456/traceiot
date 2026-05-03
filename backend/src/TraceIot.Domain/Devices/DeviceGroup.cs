using Volo.Abp.Domain.Entities.Auditing;

namespace TraceIot.Devices;

/// <summary>设备分组</summary>
public class DeviceGroup : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }

    protected DeviceGroup() { }

    public DeviceGroup(Guid id, string name, string? description = null, int sortOrder = 0)
        : base(id)
    {
        Name = name;
        Description = description;
        SortOrder = sortOrder;
    }
}
