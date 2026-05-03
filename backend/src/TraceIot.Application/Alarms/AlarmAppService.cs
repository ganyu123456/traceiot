using TraceIot.Alarms;
using TraceIot.Devices;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace TraceIot.Application.Alarms;

public class AlarmAppService : ApplicationService, IAlarmAppService
{
    private readonly IAlarmRepository _alarmRepo;
    private readonly IDeviceRepository _deviceRepo;

    public AlarmAppService(IAlarmRepository alarmRepo, IDeviceRepository deviceRepo)
    {
        _alarmRepo  = alarmRepo;
        _deviceRepo = deviceRepo;
    }

    public async Task<PagedResultDto<AlarmRecordDto>> GetListAsync(AlarmPagedRequestDto input)
    {
        var total = await _alarmRepo.GetCountAsync(
            input.DeviceId, input.AlarmType, input.IsHandled, input.StartTime, input.EndTime);

        var items = await _alarmRepo.GetPagedListAsync(
            input.SkipCount, input.MaxResultCount,
            input.DeviceId, input.AlarmType, input.IsHandled, input.StartTime, input.EndTime);

        var deviceIds  = items.Select(a => a.DeviceId).Distinct().ToList();
        var devices    = await _deviceRepo.GetListAsync(d => deviceIds.Contains(d.Id));
        var deviceMap  = devices.ToDictionary(d => d.Id, d => d.DeviceName);

        var dtos = items.Select(a =>
        {
            var dto = ObjectMapper.Map<AlarmRecord, AlarmRecordDto>(a);
            dto.DeviceName = deviceMap.GetValueOrDefault(a.DeviceId);
            return dto;
        }).ToList();

        return new PagedResultDto<AlarmRecordDto>(total, dtos);
    }

    public async Task<AlarmRecordDto> HandleAsync(Guid id, HandleAlarmDto input)
    {
        var record = await _alarmRepo.GetAsync(id);
        record.Handle(CurrentUser.Id ?? Guid.Empty, input.Note);
        await _alarmRepo.UpdateAsync(record);
        return ObjectMapper.Map<AlarmRecord, AlarmRecordDto>(record);
    }

    public async Task<int> GetUnhandledCountAsync()
    {
        return await _alarmRepo.GetCountAsync(isHandled: false);
    }
}
