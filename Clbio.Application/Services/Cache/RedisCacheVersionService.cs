using Clbio.Abstractions.Interfaces.Cache;
using Clbio.Application.Extensions;
using Clbio.Domain.Enums;
using StackExchange.Redis;

namespace Clbio.Application.Services.Cache
{
    public class RedisCacheVersionService(IConnectionMultiplexer redis) : ICacheVersionService
    {
        private readonly IDatabase _db = redis.GetDatabase();

        // ------------------------------------------------------------
        // WORKSPACE VERSION
        // ------------------------------------------------------------
        public Task<long> GetWorkspaceVersionAsync(Guid workspaceId)
            => GetOrInitVersionAsync(CacheKeys.WorkspaceVersionKey(workspaceId));

        public Task<long> BumpWorkspaceVersionAsync(Guid workspaceId)
            => _db.StringIncrementAsync(CacheKeys.WorkspaceVersionKey(workspaceId));

        // ------------------------------------------------------------
        // WORKSPACE ROLE VERSION
        // ------------------------------------------------------------
        public Task<long> GetWorkspaceRoleVersionAsync(WorkspaceRole role)
            => GetOrInitVersionAsync(CacheKeys.WorkspaceRoleVersionKey(role));

        public Task<long> BumpWorkspaceRoleVersionAsync(WorkspaceRole role)
            => _db.StringIncrementAsync(CacheKeys.WorkspaceRoleVersionKey(role));

        // ------------------------------------------------------------
        // MEMBERSHIP VERSION 
        // ------------------------------------------------------------
        public Task<long> GetMembershipVersionAsync(Guid userId, Guid workspaceId)
            => GetOrInitVersionAsync(CacheKeys.MembershipVersionKey(userId, workspaceId));

        public Task<long> IncrementMembershipVersionAsync(Guid userId, Guid workspaceId)
            => _db.StringIncrementAsync(CacheKeys.MembershipVersionKey(userId, workspaceId));

        // ------------------------------------------------------------
        // INVITATION VERSION 
        // ------------------------------------------------------------
        public Task<long> GetInvitationVersionAsync(Guid userId)
            => GetOrInitVersionAsync(CacheKeys.UserInvitationVersion(userId));

        public Task<long> IncrementInvitationVersionAsync(Guid userId)
            => _db.StringIncrementAsync(CacheKeys.UserInvitationVersion(userId));

        // ------------------------------------------------------------
        // Generic version helper
        // ------------------------------------------------------------
        private async Task<long> GetOrInitVersionAsync(string key)
        {
            var value = await _db.StringGetAsync(key);

            if (!value.HasValue)
            {
                await _db.StringSetAsync(key, 1);
                return 1;
            }

            if (long.TryParse(value.ToString(), out var parsed))
                return parsed;

            // corrupted → reset
            await _db.StringSetAsync(key, 1);
            return 1;
        }
    }
}
