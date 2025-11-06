using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.Services.Base;
using Clbio.Domain.Entities;

namespace Clbio.Application.Services
{
    public class WorkspaceService(IUnitOfWork unitOfWork, IBoardService boardService) : ServiceBase<Workspace>(unitOfWork), IWorkspaceService
    {
        private readonly IRepository<Workspace> _workspaceRepository = unitOfWork.Repository<Workspace>();
        private readonly IRepository<Board> _boardRepository = unitOfWork.Repository<Board>();
        private readonly IBoardService _boardService = boardService;
        public override async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var workspace = await _workspaceRepository.GetByIdAsync(id, ct);
            if (workspace is null) return;

            var boards = await _boardRepository.FindAsync(b => b.WorkspaceId == id, ct);
            foreach (var board in boards)
                await _boardService.DeleteAsync(board.Id, ct);

            await base.DeleteAsync(id, ct);
        }
    }
}
