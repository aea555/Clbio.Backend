using AutoMapper;
using Clbio.Abstractions.Interfaces.Infrastructure;
using Clbio.Application.DTOs.V1.Attachment;
using Clbio.Domain.Entities.V1;

namespace Clbio.Application.Mappings
{
    public class S3UrlResolver(IFileStorageService fileStorage) : IValueResolver<Attachment, ReadAttachmentDto, string>
    {
        public string Resolve(Attachment source, ReadAttachmentDto destination, string destMember, ResolutionContext context)
        {
            return string.IsNullOrEmpty(source.Url) ? string.Empty : fileStorage.GetPresignedUrl(source.Url);
        }
    }
}