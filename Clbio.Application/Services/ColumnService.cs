using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.Extensions;
using Clbio.Application.Services.Base;
using Clbio.Domain.Entities.V1;
using Clbio.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services
{
    public class ColumnService(IUnitOfWork unitOfWork, ITaskService taskService, ILogger<ColumnService>? logger = null)
        : ServiceBase<Column>(unitOfWork, logger), IColumnService
    {
        private readonly ITaskService _taskService = taskService;

        public override Task<Result> DeleteAsync(Guid id, CancellationToken ct = default) =>
            SafeExecution.ExecuteSafeAsync(async () =>
            {
                var column = await Repo<Column>().GetByIdAsync(id, false, ct) ?? throw new InvalidOperationException("Column not found");
                var tasks = await Repo<TaskItem>().FindAsync(t => t.ColumnId == id, true, ct);
                foreach (var task in tasks)
                    await _taskService.DeleteAsync(task.Id, ct);

                await base.DeleteAsync(id, ct);
            }, _logger, "COLUMN_DELETE_FAILED");
    }
}
