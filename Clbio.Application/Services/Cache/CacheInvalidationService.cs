using Clbio.Abstractions.Interfaces.Cache;
using Clbio.Application.Extensions;
using Clbio.Domain.Enums;
using StackExchange.Redis;

namespace Clbio.Application.Services.Cache
{
    public class CacheInvalidationService(ICacheService cache, ICacheVersionService versions, IConnectionMultiplexer redis) : ICacheInvalidationService
    {
        private readonly ICacheService _cache = cache;
        private readonly ICacheVersionService _versions = versions;
        private readonly IConnectionMultiplexer _redis = redis;

        private ISubscriber Sub => _redis.GetSubscriber();

        // ─────────────────────────────────────────────────────
        // USER invalidation
        // ─────────────────────────────────────────────────────
        public async Task InvalidateUser(Guid userId)
        {
            await _cache.RemoveAsync(CacheKeys.User(userId));
            await _cache.RemoveAsync(CacheKeys.NotificationCount(userId));
            await _cache.RemoveAsync(CacheKeys.UserWorkspaces(userId));

            await Sub.PublishAsync(RedisChannel.Literal(CacheChannels.UserInvalidated), userId.ToString());
        }

        // ─────────────────────────────────────────────────────
        // WORKSPACE invalidation
        // ─────────────────────────────────────────────────────
        public async Task InvalidateWorkspace(Guid workspaceId)
        {
            // bump workspace version, invalidating:
            // - workspace:vX:{id}
            // - membership:vX:*
            // - boards:ws:vX:{id}
            await _versions.BumpWorkspaceVersionAsync(workspaceId);

            await Sub.PublishAsync(RedisChannel.Literal(CacheChannels.WorkspaceInvalidated), workspaceId.ToString());
        }

        // ─────────────────────────────────────────────────────
        // MEMBERSHIP invalidation
        // ─────────────────────────────────────────────────────
        public async Task InvalidateMembership(Guid userId, Guid workspaceId)
        {
            await _versions.IncrementMembershipVersionAsync(userId, workspaceId);

            await Sub.PublishAsync(RedisChannel.Literal(CacheChannels.MembershipInvalidated),
                $"{userId}:{workspaceId}");
        }

        // ─────────────────────────────────────────────────────
        // PERMISSIONS invalidation (Workspace Role)
        // ─────────────────────────────────────────────────────
        public async Task InvalidateWorkspaceRole(WorkspaceRole role)
        {
            await _versions.BumpWorkspaceRoleVersionAsync(role);
            await Sub.PublishAsync(RedisChannel.Literal(CacheChannels.WorkspaceRoleInvalidated), role.ToString());
        }

        // ─────────────────────────────────────────────────────
        // INVITATIONS invalidation (Workspace Role)
        // ─────────────────────────────────────────────────────
        public async Task InvalidateUserInvitations(Guid userId)
        {
            await _versions.IncrementInvitationVersionAsync(userId);
            await Sub.PublishAsync(RedisChannel.Literal(CacheChannels.InvitationInvalidated), userId.ToString());
        }

        // ─────────────────────────────────────────────────────
        // Board meta invalidation
        // ─────────────────────────────────────────────────────
        public async Task InvalidateBoardMeta(Guid boardId)
        { 
            await _cache.RemoveAsync(CacheKeys.BoardMetaWorkspaceId(boardId));
            await Sub.PublishAsync(RedisChannel.Literal(CacheChannels.ColumnInvalidated), boardId.ToString());
        }
    }
}
