using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceIot.Devices;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace TraceIot.Controllers;

[Authorize]
[ApiController]
[Route("api/device-groups")]
public class DeviceGroupController : AbpControllerBase
{
    private readonly IDeviceGroupAppService _groupService;

    public DeviceGroupController(IDeviceGroupAppService groupService)
    {
        _groupService = groupService;
    }

    [HttpGet]
    public Task<PagedResultDto<DeviceGroupDto>> GetList([FromQuery] DeviceGroupPagedRequestDto input)
        => _groupService.GetListAsync(input);

    [HttpGet("all")]
    public Task<List<DeviceGroupDto>> GetAll() => _groupService.GetAllAsync();

    [HttpGet("{id:guid}")]
    public Task<DeviceGroupDto> Get(Guid id) => _groupService.GetAsync(id);

    [HttpPost]
    public Task<DeviceGroupDto> Create([FromBody] CreateUpdateDeviceGroupDto input) => _groupService.CreateAsync(input);

    [HttpPut("{id:guid}")]
    public Task<DeviceGroupDto> Update(Guid id, [FromBody] CreateUpdateDeviceGroupDto input) => _groupService.UpdateAsync(id, input);

    [HttpDelete("{id:guid}")]
    public Task Delete(Guid id) => _groupService.DeleteAsync(id);
}
