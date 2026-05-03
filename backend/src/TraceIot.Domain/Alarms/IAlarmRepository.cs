using TraceIot.Enums;
using Volo.Abp.Domain.Repositories;

namespace TraceIot.Alarms;

public interface IAlarmRepository : IRepository<AlarmRecord, Guid>
{
    Task<List<AlarmRecord>> GetPagedListAsync(
        int skipCount, int maxResultCount,
        Guid? deviceId = null,
        AlarmType? alarmType = null,
        bool? isHandled = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default);

    Task<int> GetCountAsync(
        Guid? deviceId = null,
        AlarmType? alarmType = null,
        bool? isHandled = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        CancellationToken cancellationToken = default);
}
