using AutoMapper;
using Clbio.Application.DTOs.V1.Workspace;
using Clbio.Domain.Entities.V1;

namespace Clbio.Application.Mappings.V1
{
    public class WorkspaceMappings : Profile
    {
        public WorkspaceMappings()
        {
            CreateMap<CreateWorkspaceDto, Workspace>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.OwnerId, opt => opt.Ignore())
                .ForMember(dest => dest.Owner, opt => opt.Ignore())
                .ForMember(dest => dest.Members, opt => opt.Ignore())
                .ForMember(dest => dest.Boards, opt => opt.Ignore());

            CreateMap<UpdateWorkspaceDto, Workspace>()
                .ForMember(dest => dest.OwnerId, opt => opt.Ignore())
                .ForMember(dest => dest.Owner, opt => opt.Ignore())
                .ForMember(dest => dest.Members, opt => opt.Ignore())
                .ForMember(dest => dest.Boards, opt => opt.Ignore());

            CreateMap<Workspace, ReadWorkspaceDto>()
                .ForMember(dest => dest.OwnerDisplayName, opt => opt.MapFrom(src => src.Owner.DisplayName))
                .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src => src.Members.Count))
                .ForMember(dest => dest.BoardCount, opt => opt.MapFrom(src => src.Boards.Count));
        }
    }
}
