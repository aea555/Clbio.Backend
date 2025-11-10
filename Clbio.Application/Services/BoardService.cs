using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.Extensions;
using Clbio.Application.Services.Base;
using Clbio.Domain.Entities.V1;
using Clbio.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services
{
    public class BoardService(IUnitOfWork unitOfWork, IColumnService columnService, ILogger<BoardService>? logger = null)
        : ServiceBase<Board>(unitOfWork, logger), IBoardService
    {
        private readonly IColumnService _columnService = columnService;

        public override Task<Result> DeleteAsync(Guid id, CancellationToken ct = default) =>
            SafeExecution.ExecuteSafeAsync(async () =>
            {
                var board = await Repo<Board>().GetByIdAsync(id, ct) ?? throw new InvalidOperationException("Board not found");
                var columns = await Repo<Column>().FindAsync(c => c.BoardId == id, ct);
                foreach (var column in columns)
                    await _columnService.DeleteAsync(column.Id, ct);

                await base.DeleteAsync(id, ct);
            }, _logger, "BOARD_DELETE_FAILED");
    }
}
