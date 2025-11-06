using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.Services.Base;
using Clbio.Domain.Entities;

namespace Clbio.Application.Services
{
    public class BoardService(IUnitOfWork unitOfWork, IColumnService columnService)
    : ServiceBase<Board>(unitOfWork), IBoardService
    {
        private readonly IRepository<Board> _boardRepository = unitOfWork.Repository<Board>();
        private readonly IRepository<Column> _columnRepository = unitOfWork.Repository<Column>();
        private readonly IColumnService _columnService = columnService;

        public override async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var board = await _boardRepository.GetByIdAsync(id, ct);
            if (board is null) return;

            var columns = await _columnRepository.FindAsync(c => c.BoardId == id, ct);
            foreach (var column in columns)
                await _columnService.DeleteAsync(column.Id, ct);

            await base.DeleteAsync(id, ct);
        }
    }

}
