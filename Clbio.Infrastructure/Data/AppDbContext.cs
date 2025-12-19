using Clbio.Domain.Entities.V1;
using Clbio.Domain.Entities.V1.Auth;
using Clbio.Domain.Entities.V1.Base;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

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

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(EntityBase).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");

                    var property = Expression.Property(parameter, nameof(EntityBase.IsDeleted));

                    var notExpression = Expression.Not(property);

                    var lambda = Expression.Lambda(notExpression, parameter);

                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                }
            }

            // Apply the soft delete query filter globally
            // ApplyGlobalQueryFilters(modelBuilder);

            // apply custom fluent api configs
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            foreach (var foreignKey in modelBuilder.Model.GetEntityTypes()
                .SelectMany(e => e.GetForeignKeys()))
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }

        private void SetSoftDeleteFilter<T>(ModelBuilder builder) where T : EntityBase
        {
            builder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
        }

        private static void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(EntityBase).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var property = Expression.Property(parameter, nameof(EntityBase.IsDeleted));
                    
                    var falseConstant = Expression.Constant(false);
                    var equalExpression = Expression.Equal(property, falseConstant);

                    var lambda = Expression.Lambda(equalExpression, parameter);

                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
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
                        entry.Entity.IsDeleted = false;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = utcNow;
                        break;

                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        entry.Entity.DeletedAt = utcNow;
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
        public DbSet<Attachment> Attachments { get; set; } = null!;
        public DbSet<ActivityLog> ActivityLog { get; set; } = null!;
        public DbSet<PermissionEntity> Permissions { get; set; } = null!;
        public DbSet<RoleEntity> Roles { get; set; } = null!;
        public DbSet<RolePermissionEntity> RolePermissions { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; } = null!;
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;
        public DbSet<PasswordResetAttempt> PasswordResetAttempts { get; set; } = null!;
        public DbSet<LoginAttempt> LoginAttempts { get; set; } = null!;
        public DbSet<WorkspaceInvitation> WorkspaceInvitations { get; set; } = null!;
    }
}
