using Microsoft.EntityFrameworkCore;
using TraceIot.Devices;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace TraceIot.EntityFrameworkCore.Repositories;

public class DeviceGroupRepository : EfCoreRepository<TraceIotDbContext, DeviceGroup, Guid>, IDeviceGroupRepository
{
    public DeviceGroupRepository(IDbContextProvider<TraceIotDbContext> dbContextProvider)
        : base(dbContextProvider) { }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var db = await GetDbContextAsync();
        var query = db.DeviceGroups.Where(g => g.Name == name);
        if (excludeId.HasValue)
            query = query.Where(g => g.Id != excludeId.Value);
        return await query.AnyAsync(cancellationToken);
    }
}
