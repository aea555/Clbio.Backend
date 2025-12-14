using AutoMapper;
using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Cache;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.DTOs.V1.Comment;
using Clbio.Application.Extensions;
using Clbio.Application.Interfaces.EntityServices;
using Clbio.Application.Services.Base;
using Clbio.Domain.Entities.V1;
using Clbio.Domain.Enums;
using Clbio.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services
{
    public class CommentService(
        IUnitOfWork uow,
        IMapper mapper,
        ICacheService cache,
        ICacheInvalidationService invalidator,
        INotificationAppService notificationService,
        ISocketService socketService,
        ILogger<CommentService>? logger = null)
        : ServiceBase<Comment>(uow, logger), ICommentAppService
    {
        private readonly IMapper _mapper = mapper;
        private readonly ICacheService _cache = cache;
        private readonly ICacheInvalidationService _invalidator = invalidator;
        private readonly INotificationAppService _notificationService = notificationService;
        private readonly ISocketService _socketService = socketService;
        private readonly IRepository<TaskItem> _taskRepo = uow.Repository<TaskItem>();
        private readonly IRepository<Comment> _commentRepo = uow.Repository<Comment>();
        private readonly IRepository<User> _userRepo = uow.Repository<User>();
        private readonly IRepository<WorkspaceMember> _memberRepo = uow.Repository<WorkspaceMember>();

        // ---------------------------------------------------------------------
        // GET ALL
        // ---------------------------------------------------------------------
        public async Task<Result<List<ReadCommentDto>>> GetAllAsync(Guid workspaceId, Guid taskId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var key = CacheKeys.TaskComments(taskId);

                var commentsDto = await _cache.GetOrSetAsync(
                    key,
                    async () =>
                    {
                        var taskMeta = await _taskRepo.Query()
                            .AsNoTracking()
                            .Include(t => t.Column).ThenInclude(c => c.Board)
                            .Where(t => t.Id == taskId)
                            .Select(t => new { t.Id, WorkspaceId = t.Column.Board.WorkspaceId })
                            .FirstOrDefaultAsync(ct)
                            ?? throw new InvalidOperationException("Task not found.");

                        if (taskMeta.WorkspaceId != workspaceId)
                            throw new UnauthorizedAccessException("Task does not belong to the specified workspace.");

                        var entities = await _commentRepo.Query()
                            .AsNoTracking()
                            .Where(c => c.TaskId == taskId)
                            .Include(c => c.Author) 
                            .OrderBy(c => c.CreatedAt)
                            .ToListAsync(ct);

                        return _mapper.Map<List<ReadCommentDto>>(entities);
                    },
                    TimeSpan.FromMinutes(60));

                return commentsDto ?? [];

            }, _logger, "COMMENT_LIST_FAILED");
        }

        // ---------------------------------------------------------------------
        // CREATE
        // ---------------------------------------------------------------------
        public async Task<Result<ReadCommentDto>> CreateAsync(Guid workspaceId, CreateCommentDto dto, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                // 1. Get task and its hierarchy
                var task = await _taskRepo.Query()
                    .Include(t => t.Column)
                        .ThenInclude(c => c.Board)
                    .FirstOrDefaultAsync(t => t.Id == dto.TaskId, ct)
                    ?? throw new InvalidOperationException("Task not found.");

                // Security check: Parent-Child Consistency
                if (task.Column.Board.WorkspaceId != workspaceId)
                    throw new UnauthorizedAccessException("Cannot add comment: Task is outside the workspace scope.");

                // 2. create entity
                var comment = _mapper.Map<Comment>(dto);

                await _commentRepo.AddAsync(comment, ct);
                await _uow.SaveChangesAsync(ct);
                await _invalidator.InvalidateWorkspace(workspaceId);
                await _cache.RemoveAsync(CacheKeys.TaskComments(task.Id));

                var createdCommentWithAuthor = await _commentRepo.Query()
                    .Include(c => c.Author)
                    .FirstAsync(c => c.Id == comment.Id, ct);

                var readDto = _mapper.Map<ReadCommentDto>(createdCommentWithAuthor);

                if (task.AssigneeId.HasValue && task.AssigneeId != dto.AuthorId)
                {
                    await _notificationService.SendNotificationAsync(
                        task.AssigneeId.Value,
                        "New Comment",
                        $"New comment on task '{task.Title}'",
                        ct);
                }

                // 3. Real-time
                await _socketService.SendToWorkspaceAsync(workspaceId, "CommentAdded", readDto, ct);

                return readDto;
            }, _logger, "COMMENT_CREATE_FAILED");
        }

        // ---------------------------------------------------------------------
        // DELETE
        // ---------------------------------------------------------------------
        public async Task<Result> DeleteAsync(Guid workspaceId, Guid commentId, Guid currentUserId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var comment = await _commentRepo.Query()
                    .Include(c => c.Task)
                        .ThenInclude(t => t.Column)
                            .ThenInclude(col => col.Board)
                            .Select(c => new { c.Id, c.AuthorId, c.TaskId, Task = c.Task })
                    .FirstOrDefaultAsync(c => c.Id == commentId, ct)
                    ?? throw new InvalidOperationException("Comment not found.");

                if (comment.Task.Column.Board.WorkspaceId != workspaceId)
                    throw new UnauthorizedAccessException("Comment is outside the workspace scope.");

                var currentUser = await _userRepo.GetByIdAsync(currentUserId, false, ct)
                                  ?? throw new InvalidOperationException("User not found.");

                if (currentUser.GlobalRole == GlobalRole.Admin)
                {
                    await PerformDelete(workspaceId, commentId, ct);
                    return;
                }

                if (comment.AuthorId == currentUserId)
                {
                    await PerformDelete(workspaceId, commentId, ct);
                    return;
                }

                var currentMember = await _memberRepo.Query()
                    .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == currentUserId, ct);

                if (currentMember == null)
                    throw new UnauthorizedAccessException("You are not a member of this workspace.");

                var authorMember = await _memberRepo.Query()
                    .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == comment.AuthorId, ct);

                int authorRoleValue = authorMember != null ? (int)authorMember.Role : -1;
                int currentRoleValue = (int)currentMember.Role;

                if (currentRoleValue > authorRoleValue)
                {
                    await PerformDelete(workspaceId, commentId, ct);
                    await _cache.RemoveAsync(CacheKeys.TaskComments(comment.TaskId));
                }
                else
                {
                    throw new UnauthorizedAccessException("You do not have sufficient permissions to delete this user's comment.");
                }

            }, _logger, "COMMENT_DELETE_FAILED");
        }

        private async Task PerformDelete(Guid workspaceId, Guid commentId, CancellationToken ct)
        {
            await _commentRepo.DeleteAsync(commentId, ct);
            await _uow.SaveChangesAsync(ct);
            await _invalidator.InvalidateWorkspace(workspaceId);
        }
    }
}