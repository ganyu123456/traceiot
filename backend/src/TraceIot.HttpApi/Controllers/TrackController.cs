using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TraceIot.Track;
using Volo.Abp.AspNetCore.Mvc;

namespace TraceIot.Controllers;

[Authorize]
[ApiController]
[Route("api/track")]
public class TrackController : AbpControllerBase
{
    private readonly ITrackAppService _trackService;

    public TrackController(ITrackAppService trackService)
    {
        _trackService = trackService;
    }

    [HttpGet("query")]
    public Task<TrackResultDto> Query([FromQuery] TrackQueryDto input) => _trackService.QueryAsync(input);
}
