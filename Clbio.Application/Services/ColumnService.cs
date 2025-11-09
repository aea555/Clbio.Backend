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
    public class ColumnService(IUnitOfWork unitOfWork, ITaskService taskService, ILogger<ColumnService>? logger = null)
        : ServiceBase<Column>(unitOfWork, logger), IColumnService
    {
        private readonly IRepository<Column> _columnRepository = unitOfWork.Repository<Column>();
        private readonly IRepository<TaskItem> _taskRepository = unitOfWork.Repository<TaskItem>();
        private readonly ITaskService _taskService = taskService;

        public override Task<Result> DeleteAsync(Guid id, CancellationToken ct = default) =>
            SafeExecution.ExecuteSafeAsync(async () =>
            {
                var column = await _columnRepository.GetByIdAsync(id, ct) ?? throw new InvalidOperationException("Column not found");
                var tasks = await _taskRepository.FindAsync(t => t.ColumnId == id, ct);
                foreach (var task in tasks)
                    await _taskService.DeleteAsync(task.Id, ct);

                await base.DeleteAsync(id, ct);
            }, _logger, "COLUMN_DELETE_FAILED");
    }
}
