using Microsoft.EntityFrameworkCore;
using TraceIot.Alarms;
using TraceIot.Enums;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace TraceIot.EntityFrameworkCore.Repositories;

public class AlarmRepository : EfCoreRepository<TraceIotDbContext, AlarmRecord, Guid>, IAlarmRepository
{
    public AlarmRepository(IDbContextProvider<TraceIotDbContext> dbContextProvider)
        : base(dbContextProvider) { }

    public async Task<List<AlarmRecord>> GetPagedListAsync(
        int skipCount, int maxResultCount,
        Guid? deviceId = null, AlarmType? alarmType = null, bool? isHandled = null,
        DateTime? startTime = null, DateTime? endTime = null,
        CancellationToken cancellationToken = default)
    {
        var db = await GetDbContextAsync();
        var q  = BuildQuery(db, deviceId, alarmType, isHandled, startTime, endTime);
        return await q.OrderByDescending(a => a.TriggeredAt)
                      .Skip(skipCount).Take(maxResultCount)
                      .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(
        Guid? deviceId = null, AlarmType? alarmType = null, bool? isHandled = null,
        DateTime? startTime = null, DateTime? endTime = null,
        CancellationToken cancellationToken = default)
    {
        var db = await GetDbContextAsync();
        return await BuildQuery(db, deviceId, alarmType, isHandled, startTime, endTime)
                     .CountAsync(cancellationToken);
    }

    private static IQueryable<AlarmRecord> BuildQuery(
        TraceIotDbContext db,
        Guid? deviceId, AlarmType? alarmType, bool? isHandled,
        DateTime? startTime, DateTime? endTime)
    {
        var q = db.AlarmRecords.AsQueryable();
        if (deviceId.HasValue)  q = q.Where(a => a.DeviceId  == deviceId);
        if (alarmType.HasValue) q = q.Where(a => a.AlarmType == alarmType);
        if (isHandled.HasValue) q = q.Where(a => a.IsHandled == isHandled);
        if (startTime.HasValue) q = q.Where(a => a.TriggeredAt >= startTime);
        if (endTime.HasValue)   q = q.Where(a => a.TriggeredAt <= endTime);
        return q;
    }
}
