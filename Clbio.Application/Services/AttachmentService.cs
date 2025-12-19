using AutoMapper;
using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Cache;
using Clbio.Abstractions.Interfaces.Infrastructure; // IFileStorageService buradan geliyor
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.DTOs.V1.Attachment;
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
    public class AttachmentService(
        IUnitOfWork uow,
        IMapper mapper,
        ISocketService socketService,
        ICacheInvalidationService invalidator,
        IFileStorageService fileStorage, 
        ILogger<AttachmentService>? logger = null)
        : ServiceBase<Attachment>(uow, logger), IAttachmentAppService
    {
        private readonly IRepository<TaskItem> _taskRepo = uow.Repository<TaskItem>();
        private readonly IRepository<Attachment> _attachRepo = uow.Repository<Attachment>();

        // ---------------------------------------------------------------------
        // GET ALL
        // --------------------------------------------------------------------
        public async Task<Result<List<ReadAttachmentDto>>> GetAllAsync(Guid workspaceId, Guid taskId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var task = await _taskRepo.Query()
                    .AsNoTracking()
                    .Include(t => t.Column).ThenInclude(c => c.Board)
                    .FirstOrDefaultAsync(t => t.Id == taskId, ct)
                    ?? throw new InvalidOperationException("Task not found.");

                if (task.Column.Board.WorkspaceId != workspaceId)
                    throw new UnauthorizedAccessException("Access denied.");

                var attachments = await _attachRepo.Query()
                    .AsNoTracking()
                    .Where(a => a.TaskId == taskId)
                    .Include(a => a.UploadedBy)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync(ct);

                return mapper.Map<List<ReadAttachmentDto>>(attachments);
            }, _logger, "ATTACHMENT_LIST_FAILED");
        }

        // ---------------------------------------------------------------------
        // CREATE (UPLOAD)
        // ---------------------------------------------------------------------

        public async Task<Result<List<ReadAttachmentDto>>> CreateRangeAsync(
            Guid workspaceId, 
            Guid taskId,
            CreateAttachmentDto dto, 
            Guid userId, 
            CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                if (dto.Files == null || dto.Files.Count == 0)
                    throw new InvalidOperationException("No files provided.");

                if (dto.Files.Count > 5)
                    throw new InvalidOperationException("You can upload maximum 5 files at a time.");

                var totalSize = dto.Files.Sum(f => f.Length);
                if (totalSize > 50 * 1024 * 1024)
                    throw new InvalidOperationException("Total upload size exceeds 50MB limit.");

                var task = await _taskRepo.Query()
                    .AsNoTracking()
                    .Include(t => t.Column).ThenInclude(c => c.Board)
                    .FirstOrDefaultAsync(t => t.Id == taskId, ct)
                    ?? throw new InvalidOperationException("Task not found.");

                if (task.Column.Board.WorkspaceId != workspaceId)
                    throw new UnauthorizedAccessException("Access denied.");

                var uploadTasks = dto.Files.Select(async file =>
                {
                    if (file.Length > 10 * 1024 * 1024) 
                        throw new InvalidOperationException($"File '{file.FileName}' exceeds 10MB limit.");

                    var folderPath = $"workspaces/{workspaceId}/tasks/{taskId}";
                    
                    using var stream = file.OpenReadStream();
                    
                    var url = await fileStorage.UploadAsync(
                        stream, 
                        file.FileName, 
                        file.ContentType, 
                        folderPath, 
                        ct);

                    return new Attachment
                    {
                        TaskId = taskId,
                        UploadedById = userId,
                        FileName = file.FileName,
                        ContentType = file.ContentType,
                        SizeBytes = file.Length,
                        Url = url,
                        CreatedAt = DateTime.UtcNow
                    };
                });

                var attachments = await Task.WhenAll(uploadTasks);

                await _attachRepo.AddRangeAsync(attachments, ct);
                await _uow.SaveChangesAsync(ct);
                
                await invalidator.InvalidateWorkspace(workspaceId);

                var createdIds = attachments.Select(a => a.Id).ToList();
                
                var createdEntities = await _attachRepo.Query()
                    .AsNoTracking()
                    .Include(a => a.UploadedBy)
                    .Where(a => createdIds.Contains(a.Id))
                    .ToListAsync(ct);

                return mapper.Map<List<ReadAttachmentDto>>(createdEntities);

            }, _logger, "ATTACHMENT_BATCH_CREATE_FAILED");
        }

        // ---------------------------------------------------------------------
        // DELETE
        // ---------------------------------------------------------------------
        public async Task<Result<ReadTaskItemDto>> DeleteAsync(Guid workspaceId, Guid attachmentId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var attachment = await _attachRepo.Query()
                    .AsNoTracking()
                    .Include(a => a.Task).ThenInclude(t => t.Column).ThenInclude(c => c.Board)
                    .FirstOrDefaultAsync(a => a.Id == attachmentId, ct)
                    ?? throw new InvalidOperationException("Attachment not found.");

                if (attachment.Task.Column.Board.WorkspaceId != workspaceId)
                    throw new UnauthorizedAccessException("Access denied.");

                try
                {
                    await fileStorage.DeleteAsync(attachment.Url, ct);
                }
                catch (Exception ex)
                {
                    // omit error
                    _logger?.LogError(ex, "Failed to delete file from storage. Url: {Url}", attachment.Url);
                }

                await _attachRepo.DeleteAsync(attachmentId, ct);
                await _uow.SaveChangesAsync(ct);
                
                await invalidator.InvalidateWorkspace(workspaceId);
                await socketService.SendToWorkspaceAsync(workspaceId, "WorkspaceAttachmentDeleted", new {workspaceId});

                var readDto = mapper.Map<ReadTaskItemDto>(attachment.Task);

                return readDto;

            }, _logger, "ATTACHMENT_DELETE_FAILED");
        }
    }
}