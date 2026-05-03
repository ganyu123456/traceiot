using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace TraceIot.Devices;

public class DeviceGroupAppService : ApplicationService, IDeviceGroupAppService
{
    private readonly IDeviceGroupRepository _groupRepo;
    private readonly IDeviceRepository _deviceRepo;

    public DeviceGroupAppService(IDeviceGroupRepository groupRepo, IDeviceRepository deviceRepo)
    {
        _groupRepo = groupRepo;
        _deviceRepo = deviceRepo;
    }

    public async Task<PagedResultDto<DeviceGroupDto>> GetListAsync(DeviceGroupPagedRequestDto input)
    {
        var query = await _groupRepo.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Filter))
            query = query.Where(g => g.Name.Contains(input.Filter));

        var total = query.Count();
        var items = query
            .OrderBy(g => g.SortOrder)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        var allDevices = await _deviceRepo.GetListAsync();
        var deviceCountMap = allDevices
            .Where(d => d.GroupId.HasValue)
            .GroupBy(d => d.GroupId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var dtos = items.Select(g =>
        {
            var dto = ObjectMapper.Map<DeviceGroup, DeviceGroupDto>(g);
            dto.DeviceCount = deviceCountMap.GetValueOrDefault(g.Id, 0);
            return dto;
        }).ToList();

        return new PagedResultDto<DeviceGroupDto>(total, dtos);
    }

    public async Task<List<DeviceGroupDto>> GetAllAsync()
    {
        var groups = await _groupRepo.GetListAsync();
        return ObjectMapper.Map<List<DeviceGroup>, List<DeviceGroupDto>>(groups);
    }

    public async Task<DeviceGroupDto> GetAsync(Guid id)
    {
        var group = await _groupRepo.GetAsync(id);
        return ObjectMapper.Map<DeviceGroup, DeviceGroupDto>(group);
    }

    public async Task<DeviceGroupDto> CreateAsync(CreateUpdateDeviceGroupDto input)
    {
        if (await _groupRepo.NameExistsAsync(input.Name))
            throw new Volo.Abp.UserFriendlyException($"分组名称 '{input.Name}' 已存在");

        var group = new DeviceGroup(GuidGenerator.Create(), input.Name, input.Description, input.SortOrder);
        await _groupRepo.InsertAsync(group);
        return ObjectMapper.Map<DeviceGroup, DeviceGroupDto>(group);
    }

    public async Task<DeviceGroupDto> UpdateAsync(Guid id, CreateUpdateDeviceGroupDto input)
    {
        if (await _groupRepo.NameExistsAsync(input.Name, id))
            throw new Volo.Abp.UserFriendlyException($"分组名称 '{input.Name}' 已存在");

        var group = await _groupRepo.GetAsync(id);
        group.Name = input.Name;
        group.Description = input.Description;
        group.SortOrder = input.SortOrder;
        await _groupRepo.UpdateAsync(group);
        return ObjectMapper.Map<DeviceGroup, DeviceGroupDto>(group);
    }

    public async Task DeleteAsync(Guid id)
    {
        var devices = await _deviceRepo.GetListByGroupAsync(id);
        if (devices.Any())
            throw new Volo.Abp.UserFriendlyException("该分组下还有设备，无法删除");

        await _groupRepo.DeleteAsync(id);
    }
}
