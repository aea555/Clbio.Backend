using Clbio.Application.DTOs.V1.Base;

namespace Clbio.Application.DTOs.V1.Attachment
{
    public class ReadAttachmentDto : ResponseDtoBase
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = default!;
        public string Url { get; set; } = default!;
        public string? ContentType { get; set; }
        public long SizeBytes { get; set; }
        public Guid TaskId { get; set; }

        public Guid UploadedById { get; set; }
        public string UploadedByDisplayName { get; set; } = string.Empty;
        public string? UploadedByAvatarUrl { get; set; } 
    }
}
