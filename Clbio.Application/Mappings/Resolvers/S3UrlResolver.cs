using AutoMapper;
using Clbio.Abstractions.Interfaces.Infrastructure;
using Clbio.Application.DTOs.V1.Attachment;
using Clbio.Application.DTOs.V1.User;
using Clbio.Domain.Entities.V1;

namespace Clbio.Application.Mappings.Resolvers
{
    public class S3UrlResolver(IFileStorageService fileStorage) : IValueResolver<Attachment, ReadAttachmentDto, string>
    {
        public string Resolve(Attachment source, ReadAttachmentDto destination, string destMember, ResolutionContext context)
        {
            return string.IsNullOrEmpty(source.Url) ? string.Empty : fileStorage.GetPresignedUrl(source.Url);
        }
    }

    public class S3UrlResolverUserAvatar(IFileStorageService fileStorage) : IValueResolver<User, ReadUserDto, string?>
    {
        public string? Resolve(User source, ReadUserDto destination, string? destMember, ResolutionContext context)
        {
            return string.IsNullOrEmpty(source.AvatarUrl) ? string.Empty : fileStorage.GetPresignedUrl(source.AvatarUrl);
        }
    }
}