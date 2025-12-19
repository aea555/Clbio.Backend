using Clbio.Application.DTOs.V1.Base;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Application.DTOs.V1.Attachment
{
    public class CreateAttachmentDto
    {
        [Required]
        public List<IFormFile> Files { get; set; } = [];
    }
}
