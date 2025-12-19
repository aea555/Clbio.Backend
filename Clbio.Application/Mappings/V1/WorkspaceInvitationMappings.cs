using AutoMapper;
using Clbio.Application.DTOs.V1.WorkspaceInvitation;
using Clbio.Domain.Entities.V1;

namespace Clbio.Application.Mappings.V1
{
    public class WorkspaceInvitationProfile : Profile
    {
        public WorkspaceInvitationProfile()
        {
            // --------------------------------------------------------
            // CREATE -> ENTITY
            // --------------------------------------------------------
            CreateMap<CreateWorkspaceInvitationDto, WorkspaceInvitation>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Trim().ToLowerInvariant())); 

            // --------------------------------------------------------
            // ENTITY -> READ
            // --------------------------------------------------------
            CreateMap<WorkspaceInvitation, ReadWorkspaceInvitationDto>()
                .ForMember(dest => dest.WorkspaceName, opt => opt.MapFrom(src => src.Workspace != null ? src.Workspace.Name : string.Empty))
                .ForMember(dest => dest.InviterName, opt => opt.Ignore());
        }
    }
}