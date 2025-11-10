using AutoMapper;
using Clbio.Application.DTOs.V1.TaskItem;
using Clbio.Domain.Entities.V1;

namespace Clbio.Application.Mappings.V1
{
    public class TaskItemMappings : Profile
    {
        public TaskItemMappings()
        {
            CreateMap<CreateTaskItemDto, TaskItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Column, opt => opt.Ignore())
                .ForMember(dest => dest.Assignee, opt => opt.Ignore())
                .ForMember(dest => dest.Comments, opt => opt.Ignore())
                .ForMember(dest => dest.Attachments, opt => opt.Ignore());

            CreateMap<UpdateTaskItemDto, TaskItem>()
                .ForMember(dest => dest.Column, opt => opt.Ignore())
                .ForMember(dest => dest.Assignee, opt => opt.Ignore())
                .ForMember(dest => dest.Comments, opt => opt.Ignore())
                .ForMember(dest => dest.Attachments, opt => opt.Ignore());

            CreateMap<TaskItem, ReadTaskItemDto>()
                .ForMember(dest => dest.AssigneeDisplayName, opt => opt.MapFrom(src => src.Assignee != null ? src.Assignee.DisplayName : null))
                .ForMember(dest => dest.AssigneeAvatarUrl, opt => opt.MapFrom(src => src.Assignee != null ? src.Assignee.AvatarUrl : null))
                .ForMember(dest => dest.CommentCount, opt => opt.MapFrom(src => src.Comments.Count))
                .ForMember(dest => dest.AttachmentCount, opt => opt.MapFrom(src => src.Attachments.Count));
        }
    }
}
