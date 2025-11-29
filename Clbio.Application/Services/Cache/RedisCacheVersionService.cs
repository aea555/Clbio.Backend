using Clbio.Abstractions.Interfaces.Cache;
using Clbio.Domain.Enums;
using StackExchange.Redis;

namespace Clbio.Application.Services.Cache
{
    public class RedisCacheVersionService(IConnectionMultiplexer redis) : ICacheVersionService
    {
        private readonly IConnectionMultiplexer _redis = redis;

        private StackExchange.Redis.IDatabase Db => _redis.GetDatabase();

        public Task<long> GetWorkspaceVersionAsync(Guid workspaceId)
        {
            var key = $"version:workspace:{workspaceId}";
            return GetOrInitVersion(key);
        }

        public Task<long> BumpWorkspaceVersionAsync(Guid workspaceId)
        {
            var key = $"version:workspace:{workspaceId}";
            return Db.StringIncrementAsync(key);
        }

        public Task<long> GetWorkspaceRoleVersionAsync(WorkspaceRole role)
        {
            var key = $"version:wsrole:{role}";
            return GetOrInitVersion(key);
        }

        public Task<long> BumpWorkspaceRoleVersionAsync(WorkspaceRole role)
        {
            var key = $"version:wsrole:{role}";
            return Db.StringIncrementAsync(key);
        }

        private async Task<long> GetOrInitVersion(string key)
        {
            var value = await Db.StringGetAsync(key);
            if (value.IsNullOrEmpty)
            {
                // Default version 1
                await Db.StringSetAsync(key, 1);
                return 1;
            }

            if (long.TryParse(value!, out var result))
                return result;

            // Fallback: reset to 1 if its corrupted
            await Db.StringSetAsync(key, 1);
            return 1;
        }
    }

}
