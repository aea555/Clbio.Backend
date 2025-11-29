using Clbio.Abstractions.Interfaces;
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
        public override Task<Result> DeleteAsync(Guid id, CancellationToken ct = default) =>
            SafeExecution.ExecuteSafeAsync(async () =>
            {
                var task = await Repo<TaskItem>().GetByIdAsync(id, false, ct) ?? throw new InvalidOperationException("Task not found");
                var comments = await Repo<Comment>().FindAsync(c => c.TaskId == id, false, ct);
                foreach (var comment in comments)
                    await Repo<Comment>().DeleteAsync(comment.Id, ct);

                var attachments = await Repo<Attachment>().FindAsync(a => a.TaskId == id, false, ct);
                foreach (var attachment in attachments)
                    await Repo<Attachment>().DeleteAsync(attachment.Id, ct);

                await base.DeleteAsync(id, ct);
            }, _logger, "TASK_DELETE_FAILED");
    }
}
