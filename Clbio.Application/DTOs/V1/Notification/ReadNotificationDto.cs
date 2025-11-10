using Clbio.Application.DTOs.V1.Base;

namespace Clbio.Application.DTOs.V1.Notification
{
    public class ReadNotificationDto : ResponseDtoBase
    {
        public Guid Id { get; set; }
        public bool IsRead { get; set; }
        public Guid UserId { get; set; }
    }
}
