using Clbio.Domain.Entities.V1;
using Clbio.Shared.Results;

namespace Clbio.Abstractions.Interfaces.Services
{
    public interface IColumnService : IService<Column>
    {
        Task<Result> ReorderAsync(Guid boardId, List<Guid> columnOrder, CancellationToken ct = default);
    }
}
