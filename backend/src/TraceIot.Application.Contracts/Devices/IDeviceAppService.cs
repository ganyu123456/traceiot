using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace TraceIot.Devices;

public interface IDeviceAppService : IApplicationService
{
    Task<PagedResultDto<DeviceDto>> GetListAsync(DevicePagedRequestDto input);
    Task<DeviceDto> GetAsync(Guid id);
    Task<DeviceDto> CreateAsync(CreateDeviceDto input);
    Task<DeviceDto> UpdateAsync(Guid id, UpdateDeviceDto input);
    Task DeleteAsync(Guid id);
    Task<DeviceDto> EnableAsync(Guid id);
    Task<DeviceDto> DisableAsync(Guid id);
    Task<DeviceDto> BindGroupAsync(Guid id, Guid? groupId);
}
