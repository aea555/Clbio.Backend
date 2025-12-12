using Clbio.Application.DTOs.V1.Base;
using Clbio.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Application.DTOs.V1.TaskItem
{
    public class UpdateTaskItemDto : RequestDtoBase
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Title { get; set; } = null!;

        [StringLength(2000)]
        public string? Description { get; set; }

        public int Position { get; set; }

        [Required]
        public Guid ColumnId { get; set; }

        public Guid? AssigneeId { get; set; }

        public TaskProgressStatus? ProgressStatus { get; set; }

        // Requires right privileges
        public TaskCompletionStatus? CompletionStatus { get; set; }
    }
}
