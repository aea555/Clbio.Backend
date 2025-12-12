using AutoMapper;
using Clbio.Application.DTOs.V1.User;
using Clbio.Domain.Entities.V1;

namespace Clbio.Application.Mappings.V1
{
    public class UserMappings : Profile
    {
        public UserMappings()
        {
            CreateMap<CreateUserDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.GlobalRole, opt => opt.Ignore());

            CreateMap<UpdateUserDto, User>()
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.GlobalRole, opt => opt.Ignore());
        }
    }
}
