using Volo.Abp.Application.Services;

namespace TraceIot.Track;

public interface ITrackAppService : IApplicationService
{
    Task<TrackResultDto> QueryAsync(TrackQueryDto input);
}
