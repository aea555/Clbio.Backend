using Clbio.Application.DTOs.V1.Base;

namespace Clbio.Application.DTOs.V1.Attachment
{
    public class ReadAttachmentDto : ResponseDtoBase
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = null!;
        public string Url { get; set; } = null!;
        public long SizeBytes { get; set; }
        public Guid TaskId { get; set; }

        // Optional
        public string? ContentType { get; set; }
        public Guid? UploadedById { get; set; }
        public string? UploadedByDisplayName { get; set; }
    }
}
