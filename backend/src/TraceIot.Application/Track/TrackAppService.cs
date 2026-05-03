using InfluxDB.Client;
using InfluxDB.Client.Core.Flux.Domain;
using Microsoft.Extensions.Configuration;
using TraceIot.Devices;
using TraceIot.Track;
using Volo.Abp.Application.Services;

namespace TraceIot.Application.Track;

public class TrackAppService : ApplicationService, ITrackAppService
{
    private readonly IDeviceRepository _deviceRepo;
    private readonly IInfluxDBClient _influxClient;
    private readonly string _org;
    private readonly string _bucket;

    public TrackAppService(IDeviceRepository deviceRepo, IInfluxDBClient influxClient, IConfiguration config)
    {
        _deviceRepo  = deviceRepo;
        _influxClient = influxClient;
        _org    = config["InfluxDB:Org"]    ?? "traceiot";
        _bucket = config["InfluxDB:Bucket"] ?? "gps";
    }

    public async Task<TrackResultDto> QueryAsync(TrackQueryDto input)
    {
        var device = await _deviceRepo.FindByCodeAsync(input.DeviceCode);

        var startRfc = input.StartTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        var endRfc   = input.EndTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

        // Flux 查询历史轨迹
        var flux = $@"
from(bucket: ""{_bucket}"")
  |> range(start: {startRfc}, stop: {endRfc})
  |> filter(fn: (r) => r._measurement == ""{TraceIotConsts.InfluxMeasurement}"")
  |> filter(fn: (r) => r.device_id == ""{input.DeviceCode}"")
  |> pivot(rowKey: [""_time""], columnKey: [""_field""], valueColumn: ""_value"")
  |> sort(columns: [""_time""])
  |> limit(n: {input.MaxPoints})
";

        var queryApi = _influxClient.GetQueryApi();
        var tables   = await queryApi.QueryAsync(flux, _org);
        var points   = new List<TrackPointDto>();

        foreach (var table in tables)
        {
            foreach (var record in table.Records)
            {
                points.Add(new TrackPointDto
                {
                    Lat       = GetDouble(record, "lat"),
                    Lng       = GetDouble(record, "lng"),
                    Speed     = GetDouble(record, "speed"),
                    Direction = GetDouble(record, "direction"),
                    Time      = record.GetTime()?.ToDateTimeUtc() ?? DateTime.UtcNow
                });
            }
        }

        return new TrackResultDto
        {
            DeviceCode  = input.DeviceCode,
            DeviceName  = device?.DeviceName ?? input.DeviceCode,
            TotalPoints = points.Count,
            Points      = points
        };
    }

    private static double GetDouble(FluxRecord record, string field)
    {
        var val = record.GetValueByKey(field);
        return val == null ? 0 : Convert.ToDouble(val);
    }
}
