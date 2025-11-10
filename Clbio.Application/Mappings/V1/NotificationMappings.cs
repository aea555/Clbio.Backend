using AutoMapper;
using Clbio.Application.DTOs.V1.Notification;
using Clbio.Domain.Entities.V1;

namespace Clbio.Application.Mappings.V1
{
    public class NotificationMappings : Profile
    {
        public NotificationMappings()
        {
            CreateMap<CreateNotificationDto, Notification>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore());

            CreateMap<UpdateNotificationDto, Notification>()
                .ForMember(dest => dest.Message, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore());

            CreateMap<Notification, ReadNotificationDto>();
        }
    }
}
