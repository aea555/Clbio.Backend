using Clbio.Application.DTOs.V1.Base;

namespace Clbio.Application.DTOs.V1.ActivityLog

{
    public class ReadActivityLogPagedReturnDto
    {
        public IEnumerable<ReadActivityLogDto> Items { get; set; } = null!;

        public PagedMetaDto Meta { get; set; } = null!;
    }
}