using Clbio.Application.DTOs.V1.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Application.DTOs.V1.TaskItem
{
    public class CreateTaskItemDto : RequestDtoBase
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Title { get; set; } = null!;

        [StringLength(2000)]
        public string? Description { get; set; }

        public int Position { get; set; } = 0;

        [Required]
        public Guid ColumnId { get; set; }

        public Guid? AssigneeId { get; set; }
    }
}
