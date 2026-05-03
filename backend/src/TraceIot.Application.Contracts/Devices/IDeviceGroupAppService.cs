using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace TraceIot.Devices;

public interface IDeviceGroupAppService : IApplicationService
{
    Task<PagedResultDto<DeviceGroupDto>> GetListAsync(DeviceGroupPagedRequestDto input);
    Task<List<DeviceGroupDto>> GetAllAsync();
    Task<DeviceGroupDto> GetAsync(Guid id);
    Task<DeviceGroupDto> CreateAsync(CreateUpdateDeviceGroupDto input);
    Task<DeviceGroupDto> UpdateAsync(Guid id, CreateUpdateDeviceGroupDto input);
    Task DeleteAsync(Guid id);
}
