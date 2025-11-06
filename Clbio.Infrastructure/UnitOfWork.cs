using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Domain.Entities.Base;
using Clbio.Infrastructure.Data;
using Clbio.Infrastructure.Repositories.Base;

namespace Clbio.Infrastructure
{
    public class UnitOfWork(AppDbContext context) : IUnitOfWork
    {
        private readonly AppDbContext _context = context;
        private readonly Dictionary<Type, object> _repositories = [];

        public IRepository<TEntity> Repository<TEntity>() where TEntity : EntityBase
        {
            if (!_repositories.ContainsKey(typeof(TEntity)))
                _repositories[typeof(TEntity)] = new RepositoryBase<TEntity>(_context);
            return (IRepository<TEntity>)_repositories[typeof(TEntity)];
        }

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
            => await _context.SaveChangesAsync(ct);

        public void Dispose() => GC.SuppressFinalize(this);
    }
}
