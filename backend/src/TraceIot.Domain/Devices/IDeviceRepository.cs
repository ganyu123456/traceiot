using Volo.Abp.Domain.Repositories;

namespace TraceIot.Devices;

public interface IDeviceRepository : IRepository<Device, Guid>
{
    Task<Device?> FindByCodeAsync(string deviceCode, CancellationToken cancellationToken = default);
    Task<List<Device>> GetListByGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<List<Device>> GetOnlineDevicesAsync(CancellationToken cancellationToken = default);
}
