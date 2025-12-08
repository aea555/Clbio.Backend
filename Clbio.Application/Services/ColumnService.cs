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
        ITaskService taskService,
        ICacheInvalidationService invalidator,
        IMapper mapper,
        ILogger<ColumnService>? logger = null)
        : ServiceBase<Column>(uow, logger), IColumnAppService
    {
        private readonly ITaskService _taskService = taskService;
        private readonly ICacheInvalidationService _invalidator = invalidator;
        private readonly IMapper _mapper = mapper;

        private readonly IRepository<Column> _columnRepo = uow.Repository<Column>();
        private readonly IRepository<Board> _boardRepo = uow.Repository<Board>();

        // ---------------------------------------------------------------------
        // GET ALL 
        // ---------------------------------------------------------------------
        public async Task<Result<List<ReadColumnDto>>> GetAllAsync(Guid boardId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var columns = await _columnRepo.Query()
                    .Where(c => c.BoardId == boardId)
                    .OrderBy(c => c.Position)
                    .Include(c => c.Tasks)
                    .ToListAsync(ct);

                return _mapper.Map<List<ReadColumnDto>>(columns);

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

                return _mapper.Map<ReadColumnDto>(column);

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

                _mapper.Map(dto, column);

                await _uow.SaveChangesAsync(ct);

                // Cache Invalidation
                var board = await _boardRepo.GetByIdAsync(column.BoardId, false, ct);
                if (board != null)
                    await _invalidator.InvalidateWorkspace(board.WorkspaceId);

            }, _logger, "COLUMN_UPDATE_FAILED");
        }

        // ---------------------------------------------------------------------
        // DELETE (Override)
        // ---------------------------------------------------------------------
        public override async Task<Result> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var column = await _columnRepo.GetByIdAsync(id, false, ct)
                             ?? throw new InvalidOperationException("Column not found");

                var board = await _boardRepo.GetByIdAsync(column.BoardId, false, ct);

                // delete related tasks
                var tasks = await Repo<TaskItem>().FindAsync(t => t.ColumnId == id, true, ct);
                foreach (var task in tasks)
                {
                    await _taskService.DeleteAsync(task.Id, ct);
                }

                // delete column
                await _columnRepo.DeleteAsync(id, ct);
                await _uow.SaveChangesAsync(ct);

                // Cache Invalidation
                if (board != null)
                    await _invalidator.InvalidateWorkspace(board.WorkspaceId);

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
                    await _invalidator.InvalidateWorkspace(board.WorkspaceId);

            }, _logger, "COLUMN_REORDER_FAILED");
        }
    }
}