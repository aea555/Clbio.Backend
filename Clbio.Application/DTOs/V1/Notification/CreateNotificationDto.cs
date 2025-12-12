using Clbio.Application.DTOs.V1.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Application.DTOs.V1.Notification
{
    public class CreateNotificationDto : RequestDtoBase
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        [StringLength(500, MinimumLength = 1)]
        public string MessageText { get; set; } = null!;

        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Title { get; set; } = null!;
    }
}
