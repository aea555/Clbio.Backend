using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.Extensions;
using Clbio.Application.Services.Base;
using Clbio.Domain.Entities.V1;
using Clbio.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services
{
    public class WorkspaceService(
    IUnitOfWork unitOfWork,
    IBoardService boardService,
    ILogger<WorkspaceService>? logger = null)
    : ServiceBase<Workspace>(unitOfWork, logger), IWorkspaceService
    {
        private readonly IBoardService _boardService = boardService;

        public override Task<Result<Workspace>> CreateAsync(Workspace entity, CancellationToken ct = default) =>
            SafeExecution.ExecuteSafeAsync(async () =>
            {
                var owner = await Repo<User>().GetByIdAsync(entity.OwnerId, ct)
                    ?? throw new InvalidOperationException("Owner not found");

                entity.Owner = owner;
                await Repository.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct);
                return entity;
            }, _logger, "WORKSPACE_CREATE_FAILED");

        public override Task<Result> DeleteAsync(Guid id, CancellationToken ct = default) =>
            SafeExecution.ExecuteSafeAsync(async () =>
            {
                var workspace = await Repository.GetByIdAsync(id, ct)
                    ?? throw new InvalidOperationException("Workspace not found");

                var boards = await Repo<Board>().FindAsync(b => b.WorkspaceId == id, ct);
                foreach (var board in boards)
                    await _boardService.DeleteAsync(board.Id, ct);

                await base.DeleteAsync(id, ct);
            }, _logger, "WORKSPACE_DELETE_FAILED");
    }

}
