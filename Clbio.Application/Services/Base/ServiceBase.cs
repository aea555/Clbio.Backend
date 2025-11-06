using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Domain.Entities.Base;

namespace Clbio.Application.Services.Base
{
    public class ServiceBase<T>(IUnitOfWork unitOfWork) : IService<T> where T : EntityBase
    {
        private readonly IRepository<T> _repository = unitOfWork.Repository<T>();
        private readonly IUnitOfWork _uow = unitOfWork;

        public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
            => await _repository.GetAllAsync(ct);

        public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(int page, int size, CancellationToken ct = default)
            => await _repository.GetPagedAsync(page, size, ct);

        public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _repository.GetByIdAsync(id, ct);

        public virtual async Task<T> CreateAsync(T entity, CancellationToken ct = default)
        {
            await _repository.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);
            return entity;
        }

        public virtual async Task UpdateAsync(T entity, CancellationToken ct = default)
        {
            await _repository.UpdateAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);
        }

        public virtual async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            await _repository.DeleteAsync(id, ct);
            await _uow.SaveChangesAsync(ct);
        }
    }

}
