using AutoMapper;
using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Cache;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Application.DTOs.V1.Attachment;
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
        ICacheInvalidationService invalidator,
        ILogger<AttachmentService>? logger = null)
        : ServiceBase<Attachment>(uow, logger), IAttachmentAppService
    {
        private readonly IMapper _mapper = mapper;
        private readonly ICacheInvalidationService _invalidator = invalidator;
        private readonly IRepository<TaskItem> _taskRepo = uow.Repository<TaskItem>();
        private readonly IRepository<Attachment> _attachRepo = uow.Repository<Attachment>();

        public async Task<Result<List<ReadAttachmentDto>>> GetAllAsync(Guid workspaceId, Guid taskId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var task = await _taskRepo.Query()
                    .Include(t => t.Column).ThenInclude(c => c.Board)
                    .FirstOrDefaultAsync(t => t.Id == taskId, ct)
                    ?? throw new InvalidOperationException("Task not found.");

                if (task.Column.Board.WorkspaceId != workspaceId)
                    throw new UnauthorizedAccessException("Access denied.");

                var attachments = await _attachRepo.Query()
                    .Where(a => a.TaskId == taskId)
                    .Include(a => a.UploadedBy)
                    .ToListAsync(ct);

                return _mapper.Map<List<ReadAttachmentDto>>(attachments);
            }, _logger, "ATTACHMENT_LIST_FAILED");
        }

        public async Task<Result<ReadAttachmentDto>> CreateAsync(Guid workspaceId, CreateAttachmentDto dto, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var task = await _taskRepo.Query()
                    .Include(t => t.Column).ThenInclude(c => c.Board)
                    .FirstOrDefaultAsync(t => t.Id == dto.TaskId, ct)
                    ?? throw new InvalidOperationException("Task not found.");

                if (task.Column.Board.WorkspaceId != workspaceId)
                    throw new UnauthorizedAccessException("Access denied.");

                var attachment = _mapper.Map<Attachment>(dto);

                await _attachRepo.AddAsync(attachment, ct);
                await _uow.SaveChangesAsync(ct);
                await _invalidator.InvalidateWorkspace(workspaceId);

                var created = await _attachRepo.Query()
                    .Include(a => a.UploadedBy)
                    .FirstAsync(a => a.Id == attachment.Id, ct);

                return _mapper.Map<ReadAttachmentDto>(created);
            }, _logger, "ATTACHMENT_CREATE_FAILED");
        }

        public async Task<Result> DeleteAsync(Guid workspaceId, Guid attachmentId, CancellationToken ct = default)
        {
            return await SafeExecution.ExecuteSafeAsync(async () =>
            {
                var attachment = await _attachRepo.Query()
                    .Include(a => a.Task).ThenInclude(t => t.Column).ThenInclude(c => c.Board)
                    .FirstOrDefaultAsync(a => a.Id == attachmentId, ct)
                    ?? throw new InvalidOperationException("Attachment not found.");

                if (attachment.Task.Column.Board.WorkspaceId != workspaceId)
                    throw new UnauthorizedAccessException("Access denied.");

                await _attachRepo.DeleteAsync(attachmentId, ct);
                await _uow.SaveChangesAsync(ct);
                await _invalidator.InvalidateWorkspace(workspaceId);
            }, _logger, "ATTACHMENT_DELETE_FAILED");
        }
    }
}