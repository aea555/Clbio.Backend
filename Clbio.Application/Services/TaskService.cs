using AutoMapper;
using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Cache;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.DTOs.V1.TaskItem;
using Clbio.Application.Extensions;
using Clbio.Application.Interfaces.EntityServices;
using Clbio.Application.Services.Base;
using Clbio.Domain.Entities.V1;
using Clbio.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services
{
    public class TaskService(
        IUnitOfWork uow,
        IMapper mapper,
        IActivityLogAppService activityLog,
        ICacheInvalidationService invalidator,
        ICacheService cache,
        ICacheVersionService versions,
        ISocketService socketService,
        INotificationAppService notificationService,
        ILogger<TaskService>? logger = null)
        : ServiceBase<TaskItem>(uow, logger), ITaskAppService
    {
        private readonly IMapper _mapper = mapper;
        private readonly IActivityLogAppService _activityLog = activityLog;
        private readonly ICacheInvalidationService _invalidator = invalidator;
        private readonly ICacheService _cache = cache;
        private readonly ICacheVersionService _versions = versions;
        private readonly ISocketService _socketService = socketService;
        private readonly INotificationAppService _notificationService = notificationService;

        private readonly IRepository<TaskItem> _taskRepo = uow.Repository<TaskItem>();
        private readonly IRepository<Column> _columnRepo = uow.Repository<Column>();
        private readonly IRepository<Board> _boardRepo = uow.Repository<Board>();

        public async Task<Result<List<ReadTaskItemDto>>> GetByBoardAsync(Guid workspaceId, Guid boardId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var version = await _versions.GetWorkspaceVersionAsync(workspaceId);
                var key = CacheKeys.BoardTasks(boardId, version);

                var tasks = await _cache.GetOrSetAsync(
                    key,
                    async () =>
                    {
                        var board = await _boardRepo.GetByIdAsync(boardId, false, ct);
                        if (board == null || board.WorkspaceId != workspaceId)
                            return null;

                        return await _taskRepo.Query()
                            .Include(t => t.Column)
                            .Where(t => t.Column.BoardId == boardId)
                            .OrderBy(t => t.Position)
                            .ToListAsync(ct);
                    },
                    TimeSpan.FromMinutes(10));

                return tasks == null
                    ? throw new UnauthorizedAccessException("Board not found or access denied.")
                    : _mapper.Map<List<ReadTaskItemDto>>(tasks);
            }, _logger, "TASK_LIST_FAILED");
        }

        public async new Task<Result<ReadTaskItemDto>> GetByIdAsync(Guid taskId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var taskMeta = await _taskRepo.Query()
                    .Where(t => t.Id == taskId)
                    .Select(t => new { t.Column.Board.WorkspaceId })
                    .FirstOrDefaultAsync(ct)
                    ?? throw new InvalidOperationException("Task not found.");

                var version = await _versions.GetWorkspaceVersionAsync(taskMeta.WorkspaceId);

                var key = CacheKeys.Task(taskId, version);

                var task = await _cache.GetOrSetAsync(
                    key,
                    async () => await _taskRepo.Query()
                        .Include(t => t.Column).ThenInclude(c => c.Board)
                        .FirstOrDefaultAsync(t => t.Id == taskId, ct),
                    TimeSpan.FromMinutes(10));

                return _mapper.Map<ReadTaskItemDto>(task);

            }, _logger, "TASK_GET_FAILED");
        }

        public async Task<Result<ReadTaskItemDto>> CreateAsync(Guid actorId, Guid workspaceId, CreateTaskItemDto dto, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var column = await _columnRepo.Query()
                    .Include(c => c.Board)
                    .FirstOrDefaultAsync(c => c.Id == dto.ColumnId, ct)
                    ?? throw new InvalidOperationException("Column not found.");

                if (column.Board.WorkspaceId != workspaceId)
                    throw new UnauthorizedAccessException("Cannot create task outside workspace scope.");

                var task = _mapper.Map<TaskItem>(dto);

                var maxPos = await _taskRepo.Query()
                    .Where(t => t.ColumnId == dto.ColumnId)
                    .MaxAsync(t => (int?)t.Position, ct) ?? -1;
                task.Position = maxPos + 1;

                await _taskRepo.AddAsync(task, ct);
                await _uow.SaveChangesAsync(ct);
                await _invalidator.InvalidateWorkspace(workspaceId);

                var readDto = _mapper.Map<ReadTaskItemDto>(task);

                // Real-Time
                await _socketService.SendToWorkspaceAsync(workspaceId, "TaskCreated", readDto, ct);

                await _activityLog.LogAsync(workspaceId, actorId, "Task", task.Id, "Create", $"Task '{task.Title}' created.", ct);

                return readDto;
            }, _logger, "TASK_CREATE_FAILED");
        }

        public async Task<Result> MoveTaskAsync(Guid actorId, Guid workspaceId, Guid taskId, Guid targetColumnId, int newPosition, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var task = await _taskRepo.Query()
                    .Include(t => t.Column).ThenInclude(c => c.Board)
                    .FirstOrDefaultAsync(t => t.Id == taskId, ct)
                    ?? throw new InvalidOperationException("Task not found.");

                if (task.Column.Board.WorkspaceId != workspaceId)
                    throw new UnauthorizedAccessException("Access denied.");

                if (newPosition < 0) newPosition = 0;

                // scenario 1: move to different column
                if (task.ColumnId != targetColumnId)
                {
                    // A. Shift the old column (Shift Up -1)
                    await _taskRepo.Query()
                        .Where(t => t.ColumnId == task.ColumnId && t.Position > task.Position)
                        .ExecuteUpdateAsync(s => s.SetProperty(t => t.Position, t => t.Position - 1), ct);

                    // B. Make room in the new column (Shift Down +1)
                    await _taskRepo.Query()
                        .Where(t => t.ColumnId == targetColumnId && t.Position >= newPosition)
                        .ExecuteUpdateAsync(s => s.SetProperty(t => t.Position, t => t.Position + 1), ct);

                    // C. update task
                    task.ColumnId = targetColumnId;
                    task.Position = newPosition;
                }
                // scenario 2: move in same column
                else if (task.Position != newPosition)
                {
                    var oldPosition = task.Position;

                    if (newPosition > oldPosition)
                    {
                        // Shift Up -1
                        await _taskRepo.Query()
                            .Where(t => t.ColumnId == task.ColumnId && t.Position > oldPosition && t.Position <= newPosition)
                            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Position, t => t.Position - 1), ct);
                    }
                    else
                    {
                        // Shift down + 1
                        await _taskRepo.Query()
                            .Where(t => t.ColumnId == task.ColumnId && t.Position >= newPosition && t.Position < oldPosition)
                            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Position, t => t.Position + 1), ct);
                    }

                    task.Position = newPosition;
                }

                await _uow.SaveChangesAsync(ct);

                await _activityLog.LogAsync(
                    workspaceId,
                    actorId,
                    "Task",
                    taskId,
                    "Move",
                    $"Task moved to new position {newPosition}.",
                    ct);

                await _invalidator.InvalidateWorkspace(workspaceId);

                // Real-Time
                await _socketService.SendToWorkspaceAsync(workspaceId, "TaskMoved", new
                {
                    TaskId = taskId,
                    TargetColumnId = targetColumnId,
                    NewPosition = newPosition
                }, ct);

            }, _logger, "TASK_MOVE_FAILED");
        }

        public async Task<Result> UpdateAsync(Guid workspaceId, UpdateTaskItemDto dto, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var task = await _taskRepo.Query()
                    .Include(t => t.Column).ThenInclude(c => c.Board)
                    .FirstOrDefaultAsync(t => t.Id == dto.Id, ct)
                    ?? throw new InvalidOperationException("Task not found.");

                if (task.Column.Board.WorkspaceId != workspaceId)
                    throw new UnauthorizedAccessException("Access denied.");

                _mapper.Map(dto, task);
                await _uow.SaveChangesAsync(ct);
                await _invalidator.InvalidateWorkspace(workspaceId);

                var readDto = _mapper.Map<ReadTaskItemDto>(task);
                await _socketService.SendToWorkspaceAsync(workspaceId, "TaskUpdated", readDto, ct);
            }, _logger, "TASK_UPDATE_FAILED");
        }

        public async Task<Result> DeleteAsync(Guid workspaceId, Guid taskId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var task = await _taskRepo.Query()
                    .Include(t => t.Column).ThenInclude(c => c.Board)
                    .FirstOrDefaultAsync(t => t.Id == taskId, ct)
                    ?? throw new InvalidOperationException("Task not found.");

                if (task.Column.Board.WorkspaceId != workspaceId)
                    throw new UnauthorizedAccessException("Access denied.");

                var comments = await Repo<Comment>().FindAsync(c => c.TaskId == taskId, false, ct);
                await _uow.Repository<Comment>().Query()
                    .Where(c => c.TaskId == taskId)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsDeleted, true)
                                      .SetProperty(c => c.DeletedAt, DateTime.UtcNow), ct);

                var attachments = await Repo<Attachment>().FindAsync(a => a.TaskId == taskId, false, ct);
                await _uow.Repository<Attachment>().Query()
                .Where(a => a.TaskId == taskId)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsDeleted, true)
                                      .SetProperty(a => a.DeletedAt, DateTime.UtcNow), ct);

                await _taskRepo.DeleteAsync(taskId, ct);

                await _taskRepo.Query()
                    .Where(t => t.ColumnId == task.ColumnId && t.Position > task.Position)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.Position, t => t.Position - 1), ct);

                await _uow.SaveChangesAsync(ct);
                await _invalidator.InvalidateWorkspace(workspaceId);

                await _socketService.SendToWorkspaceAsync(workspaceId, "TaskDeleted", new { Id = taskId }, ct);
            }, _logger, "TASK_DELETE_FAILED");
        }
        public async Task<Result> AssignUserAsync(Guid workspaceId, Guid taskId, Guid? assigneeId, Guid actorId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var task = await _taskRepo.Query()
                    .Include(t => t.Column)
                        .ThenInclude(c => c.Board)
                    .FirstOrDefaultAsync(t => t.Id == taskId, ct)
                     ?? throw new InvalidOperationException("Task not found.");

                if (task.Column.Board.WorkspaceId != workspaceId)
                    throw new UnauthorizedAccessException("Task is outside the workspace scope.");

                var actor = await Repo<User>().GetByIdAsync(actorId, false, ct)
                    ?? throw new InvalidOperationException("Actor user not found.");

                User? assignee = null;

                if (assigneeId.HasValue)
                {
                    // A. does user exist?
                    assignee = await Repo<User>().GetByIdAsync(assigneeId.Value, false, ct)
                               ?? throw new InvalidOperationException("Assignee user not found.");

                    // B. workspace member?
                    var isMember = await Repo<WorkspaceMember>().Query()
                        .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == assigneeId.Value, ct);

                    if (!isMember)
                        throw new InvalidOperationException($"User '{assignee.DisplayName}' is not a member of this workspace.");
                }

                if (task.AssigneeId == assigneeId) return;

                var oldAssigneeId = task.AssigneeId;
                task.AssigneeId = assigneeId;

                await _uow.SaveChangesAsync(ct);

                var action = assigneeId.HasValue ? "Assign" : "Unassign";
                var detail = assigneeId.HasValue
                    ? $"Assigned to user."
                    : "User unassigned.";

                await _activityLog.LogAsync(workspaceId, actorId, "Task", taskId, action, detail, ct);

                await _socketService.SendToWorkspaceAsync(workspaceId, "TaskAssigned", new
                {
                    TaskId = taskId,
                    AssigneeId = assigneeId,
                    AssigneeName = assignee?.DisplayName,
                    AssigneeAvatar = assignee?.AvatarUrl,
                    AssignedBy = actor.DisplayName
                }, ct);

                if (assigneeId.HasValue && assigneeId != actorId)
                {
                    await _notificationService.SendNotificationAsync(
                        assigneeId.Value,
                        "New Task Assignment",
                        $"{actor.DisplayName} assigned you to task: {task.Title}",
                        ct);
                }

                if (oldAssigneeId.HasValue && oldAssigneeId != actorId && oldAssigneeId != assigneeId)
                {
                    await _notificationService.SendNotificationAsync(
                       oldAssigneeId.Value,
                       "Task Unassigned",
                       $"{actor.DisplayName} removed you from task: {task.Title}",
                       ct);
                }
            }, _logger, "TASK_ASSIGN_FAILED");
        }
    }
}
