using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceIot.Alarms;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace TraceIot.Controllers;

[Authorize]
[ApiController]
[Route("api/alarms")]
public class AlarmController : AbpControllerBase
{
    private readonly IAlarmAppService _alarmService;

    public AlarmController(IAlarmAppService alarmService)
    {
        _alarmService = alarmService;
    }

    [HttpGet]
    public Task<PagedResultDto<AlarmRecordDto>> GetList([FromQuery] AlarmPagedRequestDto input)
        => _alarmService.GetListAsync(input);

    [HttpPut("{id:guid}/handle")]
    public Task<AlarmRecordDto> Handle(Guid id, [FromBody] HandleAlarmDto input) => _alarmService.HandleAsync(id, input);

    [HttpGet("unhandled-count")]
    public Task<int> GetUnhandledCount() => _alarmService.GetUnhandledCountAsync();
}
