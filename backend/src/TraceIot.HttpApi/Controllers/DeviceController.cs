using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceIot.Devices;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace TraceIot.Controllers;

[Authorize]
[ApiController]
[Route("api/devices")]
public class DeviceController : AbpControllerBase
{
    private readonly IDeviceAppService _deviceService;

    public DeviceController(IDeviceAppService deviceService)
    {
        _deviceService = deviceService;
    }

    [HttpGet]
    public Task<PagedResultDto<DeviceDto>> GetList([FromQuery] DevicePagedRequestDto input)
        => _deviceService.GetListAsync(input);

    [HttpGet("{id:guid}")]
    public Task<DeviceDto> Get(Guid id) => _deviceService.GetAsync(id);

    [HttpPost]
    public Task<DeviceDto> Create([FromBody] CreateDeviceDto input) => _deviceService.CreateAsync(input);

    [HttpPut("{id:guid}")]
    public Task<DeviceDto> Update(Guid id, [FromBody] UpdateDeviceDto input) => _deviceService.UpdateAsync(id, input);

    [HttpDelete("{id:guid}")]
    public Task Delete(Guid id) => _deviceService.DeleteAsync(id);

    [HttpPut("{id:guid}/enable")]
    public Task<DeviceDto> Enable(Guid id) => _deviceService.EnableAsync(id);

    [HttpPut("{id:guid}/disable")]
    public Task<DeviceDto> Disable(Guid id) => _deviceService.DisableAsync(id);

    [HttpPut("{id:guid}/bind-group")]
    public Task<DeviceDto> BindGroup(Guid id, [FromQuery] Guid? groupId) => _deviceService.BindGroupAsync(id, groupId);
}
