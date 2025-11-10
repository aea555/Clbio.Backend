using Clbio.Application.DTOs.V1.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Application.DTOs.V1.Column
{
    public class CreateColumnDto : RequestDtoBase
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = null!;

        public int Position { get; set; } = 0;

        [Required]
        public Guid BoardId { get; set; }
    }
}
