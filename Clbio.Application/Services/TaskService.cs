using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.Extensions;
using Clbio.Application.Services.Base;
using Clbio.Domain.Entities.V1;
using Clbio.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services
{
    public class TaskService(IUnitOfWork unitOfWork, ILogger<TaskService>? logger = null)
        : ServiceBase<TaskItem>(unitOfWork, logger), ITaskService
    {
        private readonly IRepository<TaskItem> _taskRepository = unitOfWork.Repository<TaskItem>();
        private readonly IRepository<Comment> _commentRepository = unitOfWork.Repository<Comment>();
        private readonly IRepository<Attachment> _attachmentRepository = unitOfWork.Repository<Attachment>();

        public override Task<Result> DeleteAsync(Guid id, CancellationToken ct = default) =>
            SafeExecution.ExecuteSafeAsync(async () =>
            {
                var task = await _taskRepository.GetByIdAsync(id, ct) ?? throw new InvalidOperationException("Task not found");
                var comments = await _commentRepository.FindAsync(c => c.TaskId == id, ct);
                foreach (var comment in comments)
                    await _commentRepository.DeleteAsync(comment.Id, ct);

                var attachments = await _attachmentRepository.FindAsync(a => a.TaskId == id, ct);
                foreach (var attachment in attachments)
                    await _attachmentRepository.DeleteAsync(attachment.Id, ct);

                await base.DeleteAsync(id, ct);
            }, _logger, "TASK_DELETE_FAILED");
    }
}
