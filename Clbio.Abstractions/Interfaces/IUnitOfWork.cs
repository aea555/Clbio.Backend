using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Domain.Entities.Base;

namespace Clbio.Abstractions.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<TEntity> Repository<TEntity>() where TEntity : EntityBase;
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
