using Clbio.Application.DTOs.V1.Base;

namespace Clbio.Application.DTOs.V1.Notification
{
    public class ReadNotificationPagedReturnDto
    {
        public IEnumerable<ReadNotificationDto> Items { get; set; } = null!;

        public PagedMetaDto Meta { get; set; } = null!;
    }
}