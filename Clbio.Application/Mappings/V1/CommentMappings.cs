using AutoMapper;
using Clbio.Application.DTOs.V1.Comment;
using Clbio.Domain.Entities.V1;

namespace Clbio.Application.Mappings.V1
{
    public class CommentMappings : Profile
    {
        public CommentMappings()
        {
            CreateMap<CreateCommentDto, Comment>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Task, opt => opt.Ignore())
                .ForMember(dest => dest.Author, opt => opt.Ignore());

            CreateMap<UpdateCommentDto, Comment>()
                .ForMember(dest => dest.TaskId, opt => opt.Ignore())
                .ForMember(dest => dest.AuthorId, opt => opt.Ignore())
                .ForMember(dest => dest.Task, opt => opt.Ignore())
                .ForMember(dest => dest.Author, opt => opt.Ignore());

            CreateMap<Comment, ReadCommentDto>()
                .ForMember(dest => dest.AuthorDisplayName, opt => opt.MapFrom(src => src.Author.DisplayName))
                .ForMember(dest => dest.AuthorAvatarUrl, opt => opt.MapFrom(src => src.Author.AvatarUrl));
        }
    }
}
