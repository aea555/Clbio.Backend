using AutoMapper;
using Clbio.Application.DTOs.V1.WorkspaceMember;
using Clbio.Domain.Entities.V1;

namespace Clbio.Application.Mappings.V1
{
    public class WorkspaceMemberMappings : Profile
    {
        public WorkspaceMemberMappings()
        {
            CreateMap<CreateWorkspaceMemberDto, WorkspaceMember>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Workspace, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore());

            CreateMap<UpdateWorkspaceMemberDto, WorkspaceMember>()
                .ForMember(dest => dest.WorkspaceId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.Workspace, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore());

            CreateMap<WorkspaceMember, ReadWorkspaceMemberDto>()
                .ForMember(dest => dest.UserDisplayName, opt => opt.MapFrom(src => src.User.DisplayName))
                .ForMember(dest => dest.UserAvatarUrl, opt => opt.MapFrom(src => src.User.AvatarUrl));
        }
    }
}
