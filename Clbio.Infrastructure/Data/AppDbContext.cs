using Clbio.Domain.Entities.V1;
using Clbio.Domain.Entities.V1.Base;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Clbio.Infrastructure.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public bool IsSoftDeleteFilterEnabled { get; private set; } = true;

        public void DisableSoftDeleteFilter()
        {
            IsSoftDeleteFilterEnabled = false;
        }

        public void EnableSoftDeleteFilter()
        {
            IsSoftDeleteFilterEnabled = true;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply the soft delete query filter globally
            ApplyGlobalQueryFilters(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            foreach (var foreignKey in modelBuilder.Model.GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys()))
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }

        private void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(EntityBase).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var isDeletedProp = Expression.Property(parameter, nameof(EntityBase.IsDeleted));
                    var dbContextProp = Expression.Property(
                        Expression.Constant(this), nameof(IsSoftDeleteFilterEnabled));

                    // Expression: !e.IsDeleted || !IsSoftDeleteFilterEnabled
                    var filter = Expression.Lambda(
                        Expression.OrElse(
                            Expression.Not(isDeletedProp),
                            Expression.Not(dbContextProp)),
                        parameter);

                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
                }
            }
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var utcNow = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<EntityBase>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = utcNow;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = utcNow;
                        break;

                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        entry.Entity.UpdatedAt = utcNow;
                        break;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Workspace> Workspaces { get; set; } = null!;
        public DbSet<WorkspaceMember> WorkspaceMembers { get; set; } = null!;
        public DbSet<Board> Boards { get; set; } = null!;
        public DbSet<Column> Columns { get; set; } = null!;
        public DbSet<TaskItem> Tasks { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;

    }
}
