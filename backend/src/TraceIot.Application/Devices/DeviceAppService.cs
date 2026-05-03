using TraceIot.Enums;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace TraceIot.Devices;

public class DeviceAppService : ApplicationService, IDeviceAppService
{
    private readonly IDeviceRepository _deviceRepo;
    private readonly IDeviceGroupRepository _groupRepo;

    public DeviceAppService(IDeviceRepository deviceRepo, IDeviceGroupRepository groupRepo)
    {
        _deviceRepo = deviceRepo;
        _groupRepo = groupRepo;
    }

    public async Task<PagedResultDto<DeviceDto>> GetListAsync(DevicePagedRequestDto input)
    {
        var query = await _deviceRepo.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Filter))
            query = query.Where(d => d.DeviceCode.Contains(input.Filter) || d.DeviceName.Contains(input.Filter));
        if (input.GroupId.HasValue)
            query = query.Where(d => d.GroupId == input.GroupId);
        if (input.Status.HasValue)
            query = query.Where(d => d.Status == input.Status);
        if (input.IsEnabled.HasValue)
            query = query.Where(d => d.IsEnabled == input.IsEnabled);

        var total = query.Count();
        var items = query
            .OrderByDescending(d => d.CreationTime)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        var groups = await _groupRepo.GetListAsync();
        var groupMap = groups.ToDictionary(g => g.Id, g => g.Name);

        var dtos = items.Select(d =>
        {
            var dto = ObjectMapper.Map<Device, DeviceDto>(d);
            if (d.GroupId.HasValue)
                dto.GroupName = groupMap.GetValueOrDefault(d.GroupId.Value);
            return dto;
        }).ToList();

        return new PagedResultDto<DeviceDto>(total, dtos);
    }

    public async Task<DeviceDto> GetAsync(Guid id)
    {
        var device = await _deviceRepo.GetAsync(id);
        var dto = ObjectMapper.Map<Device, DeviceDto>(device);
        if (device.GroupId.HasValue)
        {
            var group = await _groupRepo.FindAsync(device.GroupId.Value);
            dto.GroupName = group?.Name;
        }
        return dto;
    }

    public async Task<DeviceDto> CreateAsync(CreateDeviceDto input)
    {
        var existing = await _deviceRepo.FindByCodeAsync(input.DeviceCode);
        if (existing != null)
            throw new Volo.Abp.UserFriendlyException($"设备编号 '{input.DeviceCode}' 已存在");

        var device = new Device(GuidGenerator.Create(), input.DeviceCode, input.DeviceName, input.GroupId)
        {
            Remark = input.Remark,
            OverspeedThreshold = input.OverspeedThreshold,
            OfflineTimeoutSec = input.OfflineTimeoutSec
        };

        await _deviceRepo.InsertAsync(device);
        return ObjectMapper.Map<Device, DeviceDto>(device);
    }

    public async Task<DeviceDto> UpdateAsync(Guid id, UpdateDeviceDto input)
    {
        var device = await _deviceRepo.GetAsync(id);
        device.DeviceName = input.DeviceName;
        device.GroupId = input.GroupId;
        device.Remark = input.Remark;
        device.OverspeedThreshold = input.OverspeedThreshold;
        device.OfflineTimeoutSec = input.OfflineTimeoutSec;
        device.GeofenceEnabled = input.GeofenceEnabled;
        device.GeofenceConfig = input.GeofenceConfig;
        await _deviceRepo.UpdateAsync(device);
        return ObjectMapper.Map<Device, DeviceDto>(device);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _deviceRepo.DeleteAsync(id);
    }

    public async Task<DeviceDto> EnableAsync(Guid id)
    {
        var device = await _deviceRepo.GetAsync(id);
        device.IsEnabled = true;
        device.Status = DeviceStatus.Offline;
        await _deviceRepo.UpdateAsync(device);
        return ObjectMapper.Map<Device, DeviceDto>(device);
    }

    public async Task<DeviceDto> DisableAsync(Guid id)
    {
        var device = await _deviceRepo.GetAsync(id);
        device.IsEnabled = false;
        device.Status = DeviceStatus.Disabled;
        await _deviceRepo.UpdateAsync(device);
        return ObjectMapper.Map<Device, DeviceDto>(device);
    }

    public async Task<DeviceDto> BindGroupAsync(Guid id, Guid? groupId)
    {
        var device = await _deviceRepo.GetAsync(id);
        device.GroupId = groupId;
        await _deviceRepo.UpdateAsync(device);
        return ObjectMapper.Map<Device, DeviceDto>(device);
    }
}
