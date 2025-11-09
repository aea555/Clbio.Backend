using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.Extensions;
using Clbio.Application.Services.Base;
using Clbio.Domain.Entities.V1;
using Clbio.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services
{
    public class WorkspaceService(IUnitOfWork unitOfWork, IBoardService boardService, ILogger<WorkspaceService>? logger = null)
        : ServiceBase<Workspace>(unitOfWork, logger), IWorkspaceService
    {
        private readonly IRepository<Workspace> _workspaceRepository = unitOfWork.Repository<Workspace>();
        private readonly IRepository<Board> _boardRepository = unitOfWork.Repository<Board>();
        private readonly IBoardService _boardService = boardService;

        public override Task<Result> DeleteAsync(Guid id, CancellationToken ct = default) =>
            SafeExecution.ExecuteSafeAsync(async () =>
            {
                var workspace = await _workspaceRepository.GetByIdAsync(id, ct) ?? throw new InvalidOperationException("Workspace not found");
                var boards = await _boardRepository.FindAsync(b => b.WorkspaceId == id, ct);
                foreach (var board in boards)
                    await _boardService.DeleteAsync(board.Id, ct);

                await base.DeleteAsync(id, ct);
            }, _logger, "WORKSPACE_DELETE_FAILED");
    }
}
