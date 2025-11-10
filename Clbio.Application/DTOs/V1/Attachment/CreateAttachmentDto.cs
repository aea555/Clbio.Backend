using Clbio.Application.DTOs.V1.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Application.DTOs.V1.Attachment
{
    public class CreateAttachmentDto : RequestDtoBase
    {
        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = null!;

        [Required]
        [Url]
        public string Url { get; set; } = null!;

        [Required]
        [Range(1, long.MaxValue)]
        public long SizeBytes { get; set; }

        [Required]
        public Guid TaskId { get; set; }

        [Required]
        public Guid UploadedById { get; set; }
    }
}
