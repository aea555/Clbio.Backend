using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Application.DTOs.V1.User
{
    public class UploadAvatarDto
    {
        [Required]
        public IFormFile File { get; set; } = null!;
    }
}