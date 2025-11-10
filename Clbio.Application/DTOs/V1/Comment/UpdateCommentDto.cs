using Clbio.Application.DTOs.V1.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Application.DTOs.V1.Comment
{
    public class UpdateCommentDto : RequestDtoBase
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        [StringLength(2000, MinimumLength = 1)]
        public string Body { get; set; } = null!;
    }
}
