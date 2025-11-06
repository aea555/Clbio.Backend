using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.Services.Base;
using Clbio.Domain.Entities;

namespace Clbio.Application.Services
{
    public class ColumnService(IUnitOfWork unitOfWork, ITaskService taskService)
    : ServiceBase<Column>(unitOfWork), IColumnService
    {
        private readonly IRepository<Column> _columnRepository = unitOfWork.Repository<Column>();
        private readonly IRepository<TaskItem> _taskRepository = unitOfWork.Repository<TaskItem>();
        private readonly ITaskService _taskService = taskService;

        public override async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var column = await _columnRepository.GetByIdAsync(id, ct);
            if (column is null) return;

            var tasks = await _taskRepository.FindAsync(t => t.ColumnId == id, ct);
            foreach (var task in tasks)
                await _taskService.DeleteAsync(task.Id, ct);

            await base.DeleteAsync(id, ct);
        }
    }

}
