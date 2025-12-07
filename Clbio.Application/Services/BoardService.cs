using AutoMapper;
using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Cache;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.DTOs.V1.Board;
using Clbio.Application.Extensions;
using Clbio.Application.Interfaces.EntityServices;
using Clbio.Domain.Entities.V1;
using Clbio.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services
{
    public sealed class BoardService(
        IUnitOfWork uow,
        IColumnService columnService,
        ICacheInvalidationService invalidator,
        IMapper mapper,
        ILogger<BoardService>? logger = null) : IBoardAppService
    {
        private readonly IUnitOfWork _uow = uow;
        private readonly IRepository<Board> _boardRepo = uow.Repository<Board>();
        private readonly IRepository<Workspace> _workspaceRepo = uow.Repository<Workspace>();
        private readonly IRepository<Column> _columnRepo = uow.Repository<Column>();

        private readonly IColumnService _columnService = columnService;

        private readonly ICacheInvalidationService _invalidator = invalidator;
        private readonly IMapper _mapper = mapper;
        private readonly ILogger<BoardService>? _logger = logger;

        // ---------------------------------------------------------------------
        // GET ALL BOARDS IN WORKSPACE
        // ---------------------------------------------------------------------
        public async Task<Result<List<ReadBoardDto>>> GetAllAsync(Guid workspaceId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var boards = await _boardRepo.Query()
                    .Where(b => b.WorkspaceId == workspaceId)
                    .OrderBy(b => b.Order)
                    .Include(b => b.Columns)
                    .ToListAsync(ct);

                return boards.Select(_mapper.Map<ReadBoardDto>).ToList();

            }, _logger, "BOARD_LIST_FAILED");
        }

        // ---------------------------------------------------------------------
        // GET BOARD BY ID
        // ---------------------------------------------------------------------
        public async Task<Result<ReadBoardDto?>> GetByIdAsync(Guid boardId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var board = await _boardRepo.Query()
                    .Include(b => b.Columns)
                    .FirstOrDefaultAsync(b => b.Id == boardId, ct);

                if (board == null)
                    return null;

                return _mapper.Map<ReadBoardDto>(board);

            }, _logger, "BOARD_READ_FAILED");
        }

        // ---------------------------------------------------------------------
        // CREATE BOARD
        // ---------------------------------------------------------------------
        public async Task<Result<ReadBoardDto>> CreateAsync(CreateBoardDto dto, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                // ensure workspace exists
                var ws = await _workspaceRepo.GetByIdAsync(dto.WorkspaceId, false, ct)
                          ?? throw new InvalidOperationException("Workspace not found.");

                var board = _mapper.Map<Board>(dto);

                // assign last order
                board.Order = await _boardRepo.Query()
                    .Where(b => b.WorkspaceId == dto.WorkspaceId)
                    .CountAsync(ct);

                await _boardRepo.AddAsync(board, ct);
                await _uow.SaveChangesAsync(ct);

                // bump workspace version
                await _invalidator.InvalidateWorkspace(dto.WorkspaceId);

                return _mapper.Map<ReadBoardDto>(board);

            }, _logger, "BOARD_CREATE_FAILED");
        }

        // ---------------------------------------------------------------------
        // UPDATE BOARD
        // ---------------------------------------------------------------------
        public async Task<Result> UpdateAsync(Guid boardId, UpdateBoardDto dto, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var board = await _boardRepo.GetByIdAsync(boardId, true, ct)
                            ?? throw new InvalidOperationException("Board not found.");

                _mapper.Map(dto, board);

                await _uow.SaveChangesAsync(ct);

                await _invalidator.InvalidateWorkspace(board.WorkspaceId);

            }, _logger, "BOARD_UPDATE_FAILED");
        }

        // ---------------------------------------------------------------------
        // DELETE BOARD
        // ---------------------------------------------------------------------
        public async Task<Result> DeleteAsync(Guid boardId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var board = await _boardRepo.GetByIdAsync(boardId, false, ct)
                            ?? throw new InvalidOperationException("Board not found.");

                var workspaceId = board.WorkspaceId;

                // delete related columns
                var columns = await _columnRepo.Query()
                    .Where(c => c.BoardId == boardId)
                    .ToListAsync(ct);

                foreach (var col in columns)
                    await _columnService.DeleteAsync(col.Id, ct);

                // delete board
                await _boardRepo.DeleteAsync(boardId, ct);
                await _uow.SaveChangesAsync(ct);

                await _invalidator.InvalidateWorkspace(workspaceId);

            }, _logger, "BOARD_DELETE_FAILED");
        }

        // ---------------------------------------------------------------------
        // REORDER BOARDS
        // ---------------------------------------------------------------------
        public async Task<Result> ReorderAsync(Guid workspaceId, List<Guid> boardOrder, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                // validation
                if (boardOrder == null || boardOrder.Count == 0)
                    throw new InvalidOperationException("Board order list cannot be empty.");

                if (boardOrder.Count != boardOrder.Distinct().Count())
                    throw new InvalidOperationException("Duplicate board IDs in reorder list.");

                // load existing boards
                var boards = await _boardRepo.Query()
                    .Where(b => b.WorkspaceId == workspaceId)
                    .ToListAsync(ct);

                if (boardOrder.Count != boards.Count)
                    throw new InvalidOperationException("Reorder list count does not match number of boards in workspace.");

                var existingIds = boards.Select(b => b.Id).ToHashSet();

                if (!boardOrder.All(id => existingIds.Contains(id)))
                    throw new InvalidOperationException("Reorder contains non-existent or foreign board IDs.");

                // build lookup
                var lookup = boards.ToDictionary(b => b.Id, b => b);

                // update order
                for (int i = 0; i < boardOrder.Count; i++)
                {
                    lookup[boardOrder[i]].Order = i;
                }

                await _uow.SaveChangesAsync(ct);
                await _invalidator.InvalidateWorkspace(workspaceId);

            }, _logger, "BOARD_REORDER_FAILED");
        }
    }
}
