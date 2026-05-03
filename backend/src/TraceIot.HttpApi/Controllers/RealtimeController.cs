using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceIot.Realtime;
using Volo.Abp.AspNetCore.Mvc;

namespace TraceIot.Controllers;

[Authorize]
[ApiController]
[Route("api/realtime")]
public class RealtimeController : AbpControllerBase
{
    private readonly IRealtimeAppService _realtimeService;

    public RealtimeController(IRealtimeAppService realtimeService)
    {
        _realtimeService = realtimeService;
    }

    [HttpGet("positions")]
    public Task<List<DeviceRealtimeDto>> GetAllPositions() => _realtimeService.GetAllPositionsAsync();

    [HttpGet("position/{deviceCode}")]
    public Task<DeviceRealtimeDto?> GetPosition(string deviceCode) => _realtimeService.GetPositionAsync(deviceCode);

    [HttpGet("dashboard")]
    public Task<DashboardStatsDto> GetDashboardStats() => _realtimeService.GetDashboardStatsAsync();
}
