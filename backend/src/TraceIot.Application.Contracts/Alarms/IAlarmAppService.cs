using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace TraceIot.Alarms;

public interface IAlarmAppService : IApplicationService
{
    Task<PagedResultDto<AlarmRecordDto>> GetListAsync(AlarmPagedRequestDto input);
    Task<AlarmRecordDto> HandleAsync(Guid id, HandleAlarmDto input);
    Task<int> GetUnhandledCountAsync();
}
