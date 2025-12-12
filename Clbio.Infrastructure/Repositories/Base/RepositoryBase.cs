using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Domain.Entities.V1.Base;
using Clbio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Clbio.Infrastructure.Repositories.Base
{
    public class RepositoryBase<T>(AppDbContext context) : IRepository<T> where T : EntityBase
    {
        protected readonly AppDbContext _context = context;
        protected readonly DbSet<T> _dbSet = context.Set<T>();

        public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
            => await _dbSet.AsNoTracking().ToListAsync(cancellationToken: ct);

        public async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            int page, int pageSize, Func<IQueryable<T>,
            IOrderedQueryable<T>> orderBy,
            bool tracked = false, CancellationToken ct = default)
        {
            IQueryable<T> query = _dbSet;

            if (!tracked)
                query = query.AsNoTracking();

            query = orderBy(query);

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }

        // For custom queries
        public IQueryable<T> Query() => _dbSet.AsQueryable();

        public virtual async Task<T?> GetByIdAsync(
            Guid id,
            bool tracked = false,
            CancellationToken ct = default)
        {
            IQueryable<T> query = _dbSet;

            if (!tracked)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(e => e.Id == id, ct);
        }

        public virtual async Task<IEnumerable<T>> FindAsync(
            Expression<Func<T, bool>> predicate,
            bool tracked = false,
            CancellationToken ct = default)
        {
            IQueryable<T> query = _dbSet.Where(predicate);

            if (!tracked)
                query = query.AsNoTracking();

            return await query.ToListAsync(ct);
        }

        public virtual async Task<T> AddAsync(T entity, CancellationToken ct = default)
        {
            await _dbSet.AddAsync(entity, ct);
            return entity;
        }

        public virtual async Task UpdateAsync(T entity, CancellationToken ct = default)
        {
            var ent = await GetByIdAsync(entity.Id, true, ct);
            if (ent is null) return;

            _dbSet.Update(entity);
        }

        public virtual async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var entity = await GetByIdAsync(id, false, ct);
            if (entity is null) return;

            _dbSet.Remove(entity);
        }

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
