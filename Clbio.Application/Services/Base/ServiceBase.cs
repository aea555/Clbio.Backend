using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.Extensions;
using Clbio.Domain.Entities.V1.Base;
using Clbio.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services.Base
{
    public class ServiceBase<T>(
        IUnitOfWork unitOfWork,
        ILogger<ServiceBase<T>>? logger = null)
        : IService<T> where T : EntityBase
    {
        protected readonly IRepository<T> _repository = unitOfWork.Repository<T>();
        protected readonly IUnitOfWork _uow = unitOfWork;
        protected readonly ILogger? _logger = logger;

        // every derived service needs to have access to its own repo
        protected IRepository<T> Repository => _uow.Repository<T>();
        // helper
        protected IRepository<TRelated> Repo<TRelated>() where TRelated : EntityBase
            => _uow.Repository<TRelated>();

        public virtual Task<Result<IEnumerable<T>>> GetAllAsync(CancellationToken ct = default) =>
            SafeExecution.ExecuteSafeAsync(() => _repository.GetAllAsync(ct), _logger, "GET_ALL_FAILED");

        public virtual Task<Result<(IEnumerable<T> Items, int TotalCount)>> GetPagedAsync(
            int page, int size, CancellationToken ct = default) =>
            SafeExecution.ExecuteSafeAsync(async () =>
            {
                if (page < 1 || size < 1)
                    throw new ArgumentException("Invalid paging parameters");

                return await _repository.GetPagedAsync(page, size, ct);
            }, _logger, "GET_PAGED_FAILED");

        public virtual Task<Result<T?>> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            SafeExecution.ExecuteSafeAsync(() => _repository.GetByIdAsync(id, true, ct), _logger, "GET_BY_ID_FAILED");

        public virtual Task<Result<T>> CreateAsync(T entity, CancellationToken ct = default) =>
            SafeExecution.ExecuteSafeAsync(async () =>
            {
                await _repository.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct);
                return entity;
            }, _logger, "CREATE_FAILED");

        public virtual Task<Result> UpdateAsync(T entity, CancellationToken ct = default) =>
            SafeExecution.ExecuteSafeAsync(async () =>
            {
                await _repository.UpdateAsync(entity, ct);
                await _uow.SaveChangesAsync(ct);
            }, _logger, "UPDATE_FAILED");

        public virtual Task<Result> DeleteAsync(Guid id, CancellationToken ct = default) =>
            SafeExecution.ExecuteSafeAsync(async () =>
            {
                await _repository.DeleteAsync(id, ct);
                await _uow.SaveChangesAsync(ct);
            }, _logger, "DELETE_FAILED");
    }
}
