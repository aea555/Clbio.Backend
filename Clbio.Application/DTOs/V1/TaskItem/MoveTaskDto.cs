using Clbio.Application.DTOs.V1.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Application.DTOs.V1.TaskItem
{
    public class MoveTaskDto : RequestDtoBase
    {
        [Required]
        public Guid TargetColumnId { get; set; }
        [Range(0, 999, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public int NewPosition { get; set; }
    }
}
