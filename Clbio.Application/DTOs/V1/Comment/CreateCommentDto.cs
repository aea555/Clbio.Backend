using Clbio.Application.DTOs.V1.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Application.DTOs.V1.Comment
{
    public class CreateCommentDto : RequestDtoBase
    {
        [Required]
        [StringLength(2000, MinimumLength = 1)]
        public string Body { get; set; } = null!;

        [Required]
        public Guid TaskId { get; set; }

        [Required]
        public Guid AuthorId { get; set; }
    }
}
