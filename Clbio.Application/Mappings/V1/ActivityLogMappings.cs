using AutoMapper;
using Clbio.Application.DTOs.V1.ActivityLog;
using Clbio.Domain.Entities.V1;

namespace Clbio.Application.Mappings.V1
{
    public class ActivityLogMappings : Profile
    {
        public ActivityLogMappings()
        {
            CreateMap<CreateActivityLogDto, ActivityLog>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Workspace, opt => opt.Ignore())
                .ForMember(dest => dest.Actor, opt => opt.Ignore());

            CreateMap<ActivityLog, ReadActivityLogDto>()
                .ForMember(dest => dest.ActorDisplayName, opt => opt.MapFrom(src => src.Actor.DisplayName));
        }
    }
}
