using AutoMapper;
using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Cache;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.DTOs.V1.Column;
using Clbio.Application.Extensions;
using Clbio.Application.Interfaces.EntityServices;
using Clbio.Application.Services.Base;
using Clbio.Domain.Entities.V1;
using Clbio.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services
{
    public class ColumnService(
        IUnitOfWork uow,
        ITaskAppService taskService,
        ICacheInvalidationService invalidator,
        ICacheService cache,
        ICacheVersionService versions,
        ISocketService socketService,
        IMapper mapper,
        ILogger<ColumnService>? logger = null)
        : ServiceBase<Column>(uow, logger), IColumnAppService
    {
        private readonly ITaskAppService _taskService = taskService;
        private readonly ICacheInvalidationService _invalidator = invalidator;
        private readonly ICacheService _cache = cache;
        private readonly ICacheVersionService _versions = versions;
        private readonly ISocketService _socketService = socketService;
        private readonly IMapper _mapper = mapper;

        private readonly IRepository<Column> _columnRepo = uow.Repository<Column>();
        private readonly IRepository<Board> _boardRepo = uow.Repository<Board>();
        private readonly IRepository<TaskItem> _taskRepo = uow.Repository<TaskItem>();

        // ---------------------------------------------------------------------
        // GET ALL 
        // ---------------------------------------------------------------------
        public async Task<Result<List<ReadColumnDto>>> GetAllAsync(Guid boardId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var metaKey = CacheKeys.BoardMetaWorkspaceId(boardId);

                var workspaceId = await _cache.GetOrSetAsync(
                    metaKey,
                    async () =>
                    {
                        var wsId = await _boardRepo.Query()
                            .AsNoTracking()
                            .Where(b => b.Id == boardId)
                            .Select(b => b.WorkspaceId)
                            .FirstOrDefaultAsync(ct);

                        if (wsId == Guid.Empty)
                            throw new InvalidOperationException("Board not found.");

                        return wsId;
                    },
                    TimeSpan.FromDays(7));

                // 2. Version & Key
                var version = await _versions.GetWorkspaceVersionAsync(workspaceId);
                var key = CacheKeys.ColumnsByBoard(boardId, version);

                _logger?.LogInformation("Fetching Columns for Board: {BoardId} | WS: {WsId} | Ver: {Ver} | Key: {Key}", 
                    boardId, workspaceId, version, key);

                var columnDtos = await _cache.GetOrSetAsync(
                    key,
                    async () =>
                    {
                        var entities = await _columnRepo.Query()
                            .AsNoTracking()
                            .Where(c => c.BoardId == boardId)
                            .OrderBy(c => c.Position)
                            .ToListAsync(ct);

                        return _mapper.Map<List<ReadColumnDto>>(entities);
                    },
                    TimeSpan.FromMinutes(30));

                return columnDtos ?? [];

            }, _logger, "COLUMN_LIST_FAILED");
        }

        // ---------------------------------------------------------------------
        // CREATE
        // ---------------------------------------------------------------------
        public async Task<Result<ReadColumnDto>> CreateAsync(Guid workspaceId, CreateColumnDto dto, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var board = await _boardRepo.GetByIdAsync(dto.BoardId, false, ct)
                            ?? throw new InvalidOperationException("Board not found.");

                if (board.WorkspaceId != workspaceId)
                {
                    throw new UnauthorizedAccessException("Board does not belong to the specified workspace.");
                }

                var metaKey = CacheKeys.BoardMetaWorkspaceId(board.Id);

                await _cache.SetAsync(metaKey, board.WorkspaceId, TimeSpan.FromDays(7));

                var column = _mapper.Map<Column>(dto);

                // auto positioning
                var maxPos = await _columnRepo.Query()
                    .Where(c => c.BoardId == dto.BoardId)
                    .MaxAsync(c => (int?)c.Position, ct) ?? -1;

                column.Position = maxPos + 1;

                await _columnRepo.AddAsync(column, ct);
                await _uow.SaveChangesAsync(ct);

                // Cache Invalidation
                await _invalidator.InvalidateWorkspace(board.WorkspaceId);

                var readDto = _mapper.Map<ReadColumnDto>(column);

                await _socketService.SendToWorkspaceAsync(board.WorkspaceId, "ColumnCreated", readDto, ct);

                return readDto;
            }, _logger, "COLUMN_CREATE_FAILED");
        }

        // ---------------------------------------------------------------------
        // UPDATE
        // ---------------------------------------------------------------------
        public async Task<Result> UpdateAsync(Guid id, UpdateColumnDto dto, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                if (id != dto.Id)
                    throw new ArgumentException("ID mismatch");

                var column = await _columnRepo.GetByIdAsync(id, true, ct)
                    ?? throw new InvalidOperationException("Column not found.");

                var board = await _boardRepo.GetByIdAsync(column.BoardId, false, ct) ?? 
                    throw new InvalidOperationException("Board not found.");

                var columnDto = _mapper.Map(dto, column);

                await _uow.SaveChangesAsync(ct);

                await _invalidator.InvalidateWorkspace(board.WorkspaceId);

                var readDto = _mapper.Map<ReadColumnDto>(column);
                await _socketService.SendToWorkspaceAsync(board.WorkspaceId, "ColumnUpdated", readDto, ct);

            }, _logger, "COLUMN_UPDATE_FAILED");
        }

        // ---------------------------------------------------------------------
        // DELETE
        // ---------------------------------------------------------------------
        public async Task<Result> DeleteAsync(Guid workspaceId, Guid boardId, Guid id, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var column = await _columnRepo.GetByIdAsync(id, false, ct)
                             ?? throw new InvalidOperationException("Column not found");

                if (column.BoardId != boardId)
                {
                    throw new InvalidOperationException("Column does not belong to the specified board.");
                }

                var board = await _boardRepo.GetByIdAsync(boardId, false, ct);

                if (board == null || board.WorkspaceId != workspaceId)
                    throw new UnauthorizedAccessException("Column is outside the workspace scope.");

                // delete related tasks
                var taskIds = await _taskRepo.Query()
                    .Where(t => t.ColumnId == id)
                    .Select(t => t.Id)
                    .ToListAsync(ct);

                if (taskIds.Count != 0)
                {
                    await _uow.Repository<Comment>().Query()
                        .Where(c => taskIds.Contains(c.TaskId))
                        .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsDeleted, true)
                                                  .SetProperty(c => c.DeletedAt, DateTime.UtcNow), ct);

                    await _uow.Repository<Attachment>().Query()
                        .Where(a => taskIds.Contains(a.TaskId))
                        .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDeleted, true)
                                                  .SetProperty(a => a.DeletedAt, DateTime.UtcNow), ct);

                    await _taskRepo.Query()
                        .Where(t => t.ColumnId == id)
                        .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsDeleted, true)
                                                  .SetProperty(t => t.DeletedAt, DateTime.UtcNow), ct);

                    var keysToRemove = taskIds
                        .Select(tid => CacheKeys.TaskMetaWorkspaceId(tid))
                        .ToList();

                    if (keysToRemove.Count > 0)
                        await _cache.RemoveAllAsync(keysToRemove);
                }

                await _columnRepo.DeleteAsync(id, ct);
                await _uow.SaveChangesAsync(ct);
                await _invalidator.InvalidateWorkspace(workspaceId);
                await _socketService.SendToWorkspaceAsync(workspaceId, "ColumnDeleted", new { ColumnId = id, BoardId = board.Id }, ct);
            }, _logger, "COLUMN_DELETE_FAILED");
        }

        // ---------------------------------------------------------------------
        // REORDER
        // ---------------------------------------------------------------------
        public async Task<Result> ReorderAsync(Guid boardId, List<Guid> columnOrder, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var columns = await _columnRepo.Query()
                    .Where(c => c.BoardId == boardId)
                    .ToListAsync(ct);

                if (columnOrder.Count != columns.Count)
                    throw new InvalidOperationException("Column count mismatch.");

                var existingIds = columns.Select(c => c.Id).ToHashSet();
                if (!columnOrder.All(id => existingIds.Contains(id)) || columnOrder.Distinct().Count() != columnOrder.Count)
                    throw new InvalidOperationException("Invalid column reorder list.");

                var lookup = columns.ToDictionary(c => c.Id);

                for (int i = 0; i < columnOrder.Count; i++)
                {
                    if (lookup.TryGetValue(columnOrder[i], out var col))
                    {
                        col.Position = i;
                    }
                }

                await _uow.SaveChangesAsync(ct);

                // Cache Invalidation
                var board = await _boardRepo.GetByIdAsync(boardId, false, ct);
                if (board != null)
                {
                    await _invalidator.InvalidateWorkspace(board.WorkspaceId);
                    await _socketService.SendToWorkspaceAsync(board.WorkspaceId, "ColumnReordered", new { BoardId = boardId, ColumnOrder = columnOrder }, ct);
                }

            }, _logger, "COLUMN_REORDER_FAILED");
        }
    }
}