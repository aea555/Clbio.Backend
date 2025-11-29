using Clbio.Domain.Enums;
using Clbio.Shared.Results;

namespace Clbio.Abstractions.Interfaces.Services
{
    public interface IUserPermissionService
    {
        Task<Result<bool>> HasPermissionAsync(
            Guid userId,
            Permission permission,
            Guid? workspaceId = null,
            CancellationToken ct = default);
    }
}
