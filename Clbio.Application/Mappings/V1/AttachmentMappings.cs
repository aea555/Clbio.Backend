using AutoMapper;
using Clbio.Application.DTOs.V1.Attachment;
using Clbio.Domain.Entities.V1;

namespace Clbio.Application.Mappings.V1
{
    public class AttachmentMappings : Profile
    {
        public AttachmentMappings()
        {
            CreateMap<CreateAttachmentDto, Attachment>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Task, opt => opt.Ignore());

            CreateMap<Attachment, ReadAttachmentDto>()
                .ForMember(dest => dest.UploadedByDisplayName, opt => opt.MapFrom(src => src.UploadedBy.DisplayName))
                .ForMember(dest => dest.UploadedById, opt => opt.MapFrom(src => src.UploadedById))
                .ForMember(dest => dest.ContentType, opt => opt.MapFrom(src => src.ContentType));
        }
    }
}
