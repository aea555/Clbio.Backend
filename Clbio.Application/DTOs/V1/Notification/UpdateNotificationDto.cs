using Clbio.Application.DTOs.V1.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Application.DTOs.V1.Notification
{
    public class UpdateNotificationDto : RequestDtoBase
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public bool IsRead { get; set; }
    }
}
