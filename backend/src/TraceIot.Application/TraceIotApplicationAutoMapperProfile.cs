using AutoMapper;
using TraceIot.Alarms;
using TraceIot.Devices;

namespace TraceIot;

public class TraceIotApplicationAutoMapperProfile : Profile
{
    public TraceIotApplicationAutoMapperProfile()
    {
        CreateMap<DeviceGroup, DeviceGroupDto>();
        CreateMap<Device, DeviceDto>();
        CreateMap<AlarmRecord, AlarmRecordDto>();
    }
}
