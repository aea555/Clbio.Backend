using AutoMapper;
using Clbio.Application.DTOs.V1.Attachment;
using Clbio.Domain.Entities.V1;

namespace Clbio.Application.Mappings.V1
{
    public class AttachmentMappings : Profile
    {
        public AttachmentMappings()
        {
            CreateMap<Attachment, ReadAttachmentDto>()
                .ForMember(dest => dest.UploadedByDisplayName, opt => opt.MapFrom(src => src.UploadedBy != null ? src.UploadedBy.DisplayName : "Unknown"))
                .ForMember(dest => dest.UploadedByAvatarUrl, opt => opt.MapFrom(src => src.UploadedBy != null ? src.UploadedBy.AvatarUrl : null))
                .ForMember(dest => dest.Url, opt => opt.MapFrom<S3UrlResolver>());
        }
    }
}
