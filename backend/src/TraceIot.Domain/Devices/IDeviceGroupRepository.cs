using Volo.Abp.Domain.Repositories;

namespace TraceIot.Devices;

public interface IDeviceGroupRepository : IRepository<DeviceGroup, Guid>
{
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
