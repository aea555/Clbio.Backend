using Clbio.Abstractions.Interfaces.Cache;
using Clbio.Application.Extensions;
using Clbio.Domain.Enums;

namespace Clbio.Application.Services.Cache
{
    public class CacheInvalidationService(ICacheService cache) : ICacheInvalidationService
    {
        private readonly ICacheService _cache = cache;

        // ─────────────────────────────────────────────────────
        // USER invalidation
        // ─────────────────────────────────────────────────────
        public async Task InvalidateUser(Guid userId)
        {
            await _cache.RemoveAsync(CacheKeys.User(userId));
            await _cache.RemoveAsync(CacheKeys.UserWorkspaces(userId));
        }

        // ─────────────────────────────────────────────────────
        // WORKSPACE invalidation
        // ─────────────────────────────────────────────────────
        public async Task InvalidateWorkspace(Guid workspaceId)
        {
            await _cache.RemoveAsync(CacheKeys.Workspace(workspaceId));
            await _cache.RemoveAsync(CacheKeys.BoardsByWorkspace(workspaceId));
            // If using membership-keys as pattern:
            // Can't delete by pattern natively with IDistributedCache,
            // but you can publish an event via Redis Pub/Sub later.
        }

        // ─────────────────────────────────────────────────────
        // MEMBERSHIP invalidation
        // ─────────────────────────────────────────────────────
        public async Task InvalidateMembership(Guid userId, Guid workspaceId)
        {
            await _cache.RemoveAsync(CacheKeys.Membership(userId, workspaceId));
        }

        // ─────────────────────────────────────────────────────
        // PERMISSIONS invalidation (Workspace Role)
        // ─────────────────────────────────────────────────────
        public async Task InvalidateWorkspaceRole(WorkspaceRole role)
        {
            await _cache.RemoveAsync(CacheKeys.RolePermissions(role));
        }
    }
}
