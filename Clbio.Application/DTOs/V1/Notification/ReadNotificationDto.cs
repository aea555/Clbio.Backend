using Clbio.Application.DTOs.V1.Base;

namespace Clbio.Application.DTOs.V1.Notification
{
    public class ReadNotificationDto : ResponseDtoBase
    {
        public Guid Id { get; set; }
        public string MessageText { get; set; } = null!;
        public string Title { get; set; } = default!;
        public bool IsRead { get; set; } = false;
    }
}
