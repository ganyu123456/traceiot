using Microsoft.EntityFrameworkCore;
using TraceIot.Devices;
using TraceIot.Enums;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace TraceIot.EntityFrameworkCore.Repositories;

public class DeviceRepository : EfCoreRepository<TraceIotDbContext, Device, Guid>, IDeviceRepository
{
    public DeviceRepository(IDbContextProvider<TraceIotDbContext> dbContextProvider)
        : base(dbContextProvider) { }

    public async Task<Device?> FindByCodeAsync(string deviceCode, CancellationToken cancellationToken = default)
    {
        var db = await GetDbContextAsync();
        return await db.Devices.FirstOrDefaultAsync(d => d.DeviceCode == deviceCode, cancellationToken);
    }

    public async Task<List<Device>> GetListByGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var db = await GetDbContextAsync();
        return await db.Devices.Where(d => d.GroupId == groupId).ToListAsync(cancellationToken);
    }

    public async Task<List<Device>> GetOnlineDevicesAsync(CancellationToken cancellationToken = default)
    {
        var db = await GetDbContextAsync();
        return await db.Devices.Where(d => d.Status == DeviceStatus.Online).ToListAsync(cancellationToken);
    }
}
