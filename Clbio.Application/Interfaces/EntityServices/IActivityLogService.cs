using Clbio.Application.DTOs.V1.ActivityLog;
using Clbio.Shared.Results;

namespace Clbio.Application.Interfaces.EntityServices
{
    public interface IActivityLogAppService
    {
        // paginated listing
        Task<Result<(IEnumerable<ReadActivityLogDto> Items, int TotalCount)>> GetPagedAsync(
            Guid workspaceId,
            int page,
            int pageSize,
            CancellationToken ct = default);
    }
}