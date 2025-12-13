using AutoMapper;
using Clbio.Application.DTOs.V1.Auth;
using Clbio.Application.DTOs.V1.User;
using Clbio.Application.DTOs.V1.Workspace;
using Clbio.Domain.Entities.V1;

namespace Clbio.Application.Mappings
{
    public class GeneralProfile : Profile
    {
        public GeneralProfile()
        {
            CreateMap<User, ReadUserDto>();

            CreateMap<RegisterRequestDto, User>();
            
            CreateMap<Workspace, ReadWorkspaceDto>()
                .ForMember(dest => dest.OwnerDisplayName, opt => opt.MapFrom(src => src.Owner.DisplayName)) 
                .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src => src.Members.Count));

            CreateMap<CreateWorkspaceDto, Workspace>()
                .ForMember(dest => dest.Status, opt => opt.Ignore()) 
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            CreateMap<UpdateWorkspaceDto, Workspace>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null)); 
        }
    }
}