using AutoMapper;
using Clbio.Application.DTOs.V1.TaskItem;
using Clbio.Domain.Entities.V1;
using Clbio.Domain.Enums;

namespace Clbio.Application.Mappings.V1
{
    public class TaskItemMappings : Profile
    {
        public TaskItemMappings()
        {
            // Create
            CreateMap<CreateTaskItemDto, TaskItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Column, opt => opt.Ignore())
                .ForMember(dest => dest.Assignee, opt => opt.Ignore())
                .ForMember(dest => dest.Comments, opt => opt.Ignore())
                .ForMember(dest => dest.Attachments, opt => opt.Ignore())
                .ForMember(dest => dest.ProgressStatus, opt => opt.MapFrom(_ => TaskProgressStatus.Assigned))
                .ForMember(dest => dest.CompletionStatus, opt => opt.MapFrom(_ => TaskCompletionStatus.Active));

            // Update
            CreateMap<UpdateTaskItemDto, TaskItem>()
                .ForMember(dest => dest.Column, opt => opt.Ignore())
                .ForMember(dest => dest.Assignee, opt => opt.Ignore())
                .ForMember(dest => dest.Comments, opt => opt.Ignore())
                .ForMember(dest => dest.Attachments, opt => opt.Ignore())
                // Only update ProgressStatus or CompletionStatus if specified
                .ForMember(dest => dest.ProgressStatus, opt => opt.Condition(src => src.ProgressStatus.HasValue))
                .ForMember(dest => dest.CompletionStatus, opt => opt.Condition(src => src.CompletionStatus.HasValue));

            // Read DTO
            CreateMap<TaskItem, ReadTaskItemDto>()
                .ForMember(dest => dest.AssigneeDisplayName,
                    opt => opt.MapFrom(src => src.Assignee != null ? src.Assignee.DisplayName : null))
                .ForMember(dest => dest.AssigneeAvatarUrl,
                    opt => opt.MapFrom(src => src.Assignee != null ? src.Assignee.AvatarUrl : null))
                .ForMember(dest => dest.CommentCount, opt => opt.MapFrom(src => src.Comments.Count))
                .ForMember(dest => dest.AttachmentCount, opt => opt.MapFrom(src => src.Attachments.Count))
                // expose readable status info
                .ForMember(dest => dest.ProgressStatus, opt => opt.MapFrom(src => src.ProgressStatus))
                .ForMember(dest => dest.CompletionStatus, opt => opt.MapFrom(src => src.CompletionStatus));
        }
    }

}
