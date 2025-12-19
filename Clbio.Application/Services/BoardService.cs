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
        IActivityLogAppService activityLog,
        ICacheInvalidationService invalidator,
        ICacheService cache,
        ICacheVersionService versions,
        ISocketService socketService,
        IMapper mapper,
        ILogger<BoardService>? logger = null) : IBoardAppService
    {
        private readonly IUnitOfWork _uow = uow;
        private readonly IRepository<Board> _boardRepo = uow.Repository<Board>();
        private readonly IRepository<Column> _columnRepo = uow.Repository<Column>();

        private readonly IActivityLogAppService _activityLog = activityLog;
        private readonly ICacheInvalidationService _invalidator = invalidator;
        private readonly ICacheService _cache = cache;
        private readonly ICacheVersionService _versions = versions;
        private readonly ISocketService _socketService = socketService;
        private readonly IMapper _mapper = mapper;
        private readonly ILogger<BoardService>? _logger = logger;

        // ---------------------------------------------------------------------
        // GET ALL BOARDS IN WORKSPACE
        // ---------------------------------------------------------------------
        public async Task<Result<List<ReadBoardDto>>> GetAllAsync(Guid workspaceId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var version = await _versions.GetWorkspaceVersionAsync(workspaceId);
                var key = CacheKeys.BoardsByWorkspace(workspaceId, version);

                var boardDtos = await _cache.GetOrSetAsync(
                    key,
                    async () =>
                    {
                        var entities = await _boardRepo.Query()
                            .AsNoTracking()
                            .Where(b => b.WorkspaceId == workspaceId)
                            .OrderBy(b => b.Order)
                            .ToListAsync(ct);

                        return _mapper.Map<List<ReadBoardDto>>(entities);
                    },
                    TimeSpan.FromMinutes(30));

                return boardDtos ?? [];

            }, _logger, "BOARD_LIST_FAILED");
        }

        // ---------------------------------------------------------------------
        // GET BOARD BY ID
        // ---------------------------------------------------------------------
        public async Task<Result<ReadBoardDto?>> GetByIdAsync(Guid workspaceId, Guid boardId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var metaKey = CacheKeys.BoardMetaWorkspaceId(boardId);

                var actualWorkspaceId = await _cache.GetOrSetAsync(
                    metaKey,
                    async () =>
                    {
                        var wsId = await _boardRepo.Query()
                            .AsNoTracking()
                            .Where(b => b.Id == boardId)
                            .Select(b => b.WorkspaceId)
                            .FirstOrDefaultAsync(ct);

                        if (wsId == Guid.Empty)
                            return Guid.Empty;

                        return wsId;
                    },
                    TimeSpan.FromDays(7));

                if (actualWorkspaceId == Guid.Empty)
                    return null;

                if (actualWorkspaceId != workspaceId)
                    throw new UnauthorizedAccessException("Access to this board is denied under the current workspace.");

                var version = await _versions.GetWorkspaceVersionAsync(workspaceId);
                var key = CacheKeys.Board(boardId, version);

                var boardDto = await _cache.GetOrSetAsync(
                    key,
                    async () =>
                    {
                        var entity = await _boardRepo.Query()
                            .AsNoTracking()
                            .FirstOrDefaultAsync(b => b.Id == boardId, ct);

                        return _mapper.Map<ReadBoardDto>(entity);
                    },
                    TimeSpan.FromMinutes(30));

                return boardDto;
            }, _logger, "BOARD_READ_FAILED");
        }

        // ---------------------------------------------------------------------
        // CREATE BOARD
        // ---------------------------------------------------------------------
        public async Task<Result<ReadBoardDto>> CreateAsync(Guid actorId, CreateBoardDto dto, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var board = _mapper.Map<Board>(dto);

                // assign last order
                board.Order = await _boardRepo.Query()
                    .Where(b => b.WorkspaceId == dto.WorkspaceId)
                    .CountAsync(ct);

                await _boardRepo.AddAsync(board, ct);
                await _uow.SaveChangesAsync(ct);

                await _activityLog.LogAsync(dto.WorkspaceId, actorId, "Board", board.Id, "Create", $"Board '{board.Name}' created.", ct);

                // bump workspace version
                await _invalidator.InvalidateWorkspace(dto.WorkspaceId);
                await _cache.RemoveAsync(CacheKeys.BoardMetaWorkspaceId(board.Id));

                var readDto = _mapper.Map<ReadBoardDto>(board);

                await _socketService.SendToWorkspaceAsync(board.WorkspaceId, "BoardCreated", readDto, ct);

                return readDto;

            }, _logger, "BOARD_CREATE_FAILED");
        }

        // ---------------------------------------------------------------------
        // UPDATE BOARD
        // ---------------------------------------------------------------------
        public async Task<Result> UpdateAsync(Guid workspaceId, Guid boardId, UpdateBoardDto dto, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var board = await _boardRepo.GetByIdAsync(boardId, true, ct)
                            ?? throw new InvalidOperationException("Board not found.");

                if (board.WorkspaceId != workspaceId)
                    throw new UnauthorizedAccessException("Board does not belong to the specified workspace.");

                _mapper.Map(dto, board);

                await _uow.SaveChangesAsync(ct);

                await _invalidator.InvalidateWorkspace(board.WorkspaceId);

                var readDto = _mapper.Map<ReadBoardDto>(board);

                await _socketService.SendToWorkspaceAsync(workspaceId, "BoardUpdated", readDto, ct);
            }, _logger, "BOARD_UPDATE_FAILED");
        }

        // ---------------------------------------------------------------------
        // DELETE BOARD
        // ---------------------------------------------------------------------
        public async Task<Result> DeleteAsync(Guid actorId, Guid workspaceId, Guid boardId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var board = await _boardRepo.GetByIdAsync(boardId, false, ct)
                            ?? throw new InvalidOperationException("Board not found.");

                if (board.WorkspaceId != workspaceId)
                    throw new UnauthorizedAccessException("Board does not belong to the specified workspace.");

                var columnIds = await _columnRepo.Query()
                    .Where(c => c.BoardId == boardId)
                    .Select(c => c.Id)
                    .ToListAsync(ct);

                if (columnIds.Count != 0)
                {
                    var taskIds = await uow.Repository<TaskItem>().Query()
                        .Where(t => columnIds.Contains(t.ColumnId))
                        .Select(t => t.Id)
                        .ToListAsync(ct);

                    if (taskIds.Count != 0)
                    {
                        var utcNow = DateTime.UtcNow;

                        await uow.Repository<Comment>().Query()
                            .Where(c => taskIds.Contains(c.TaskId))
                            .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsDeleted, true)
                                                      .SetProperty(c => c.DeletedAt, utcNow), ct);

                        await uow.Repository<Attachment>().Query()
                            .Where(a => taskIds.Contains(a.TaskId))
                            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDeleted, true)
                                                      .SetProperty(a => a.DeletedAt, utcNow), ct);

                        await uow.Repository<TaskItem>().Query()
                            .Where(t => taskIds.Contains(t.Id))
                            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsDeleted, true)
                                                      .SetProperty(t => t.DeletedAt, utcNow), ct);
                    }

                    await _columnRepo.Query()
                        .Where(c => columnIds.Contains(c.Id))
                        .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsDeleted, true)
                                                  .SetProperty(c => c.DeletedAt, DateTime.UtcNow), ct);
                }

                // delete board
                await _boardRepo.DeleteAsync(boardId, ct);
                await _uow.SaveChangesAsync(ct);

                await _activityLog.LogAsync(workspaceId, actorId, "Board", boardId, "Delete", "Board deleted.", ct);

                await _invalidator.InvalidateWorkspace(workspaceId);
                await _cache.RemoveAsync(CacheKeys.BoardMetaWorkspaceId(boardId));

                await _socketService.SendToWorkspaceAsync(workspaceId, "BoardDeleted", new { Id = boardId }, ct);
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

                await _socketService.SendToWorkspaceAsync(workspaceId, "BoardReordered", boardOrder, ct);
            }, _logger, "BOARD_REORDER_FAILED");
        }

        public async Task<Result<List<ReadBoardDto>>> SearchAsync(Guid workspaceId, string? searchTerm, int maxResults = 10, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                if (maxResults > 20) maxResults = 20; 
                if (maxResults < 1) maxResults = 10;

                _logger?.LogInformation("Searching for '{Term}' in Workspace '{WsId}'", searchTerm, workspaceId);

                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    var allResult = await GetAllAsync(workspaceId, ct);
                    if (!allResult.Success) return []; 

                    return allResult.Value
                        .OrderByDescending(b => b.UpdatedAt ?? b.CreatedAt) 
                        .Take(maxResults)
                        .ToList();
                }

                // SENARYO 2: Arama yapılıyor
                var term = searchTerm.Trim();

                var entities = await _boardRepo.Query()
                    .AsNoTracking()
                    .Where(b => b.WorkspaceId == workspaceId && 
                                EF.Functions.Like(b.Name, $"%{term}%"))
                    .OrderByDescending(b => b.UpdatedAt ?? b.CreatedAt)
                    .Take(maxResults) 
                    .ToListAsync(ct);
                
                return _mapper.Map<List<ReadBoardDto>>(entities);

            }, _logger, "BOARD_SEARCH_FAILED");
        }
    }
}
