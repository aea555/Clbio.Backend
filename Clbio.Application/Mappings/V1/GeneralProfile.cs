using AutoMapper;
using Clbio.Application.DTOs.V1.Auth;
using Clbio.Application.DTOs.V1.User;
using Clbio.Application.DTOs.V1.Workspace;
using Clbio.Application.Mappings.Resolvers;
using Clbio.Domain.Entities.V1;

namespace Clbio.Application.Mappings
{
    public class GeneralProfile : Profile
    {
        public GeneralProfile()
        {
            CreateMap<User, ReadUserDto>()
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom<S3UrlResolverUserAvatar>());

            CreateMap<RegisterRequestDto, User>();
        }
    }
}