using Clbio.Application.DTOs.V1.Base;
using Clbio.Domain.Enums;

namespace Clbio.Application.DTOs.V1.TaskItem
{
    public class ReadTaskItemDto : ResponseDtoBase
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int Position { get; set; }
        public Guid ColumnId { get; set; }

        public Guid? AssigneeId { get; set; }
        public string? AssigneeDisplayName { get; set; }
        public string? AssigneeAvatarUrl { get; set; }
        public TaskProgressStatus ProgressStatus { get; set; }
        public TaskCompletionStatus CompletionStatus { get; set; }

        // Optional 
        public int CommentCount { get; set; }
        public int AttachmentCount { get; set; }
    }
}
