using Clbio.Abstractions.Interfaces.Services;
using StackExchange.Redis;

namespace Clbio.Infrastructure.Services
{
    public class RedisPresenceService(IConnectionMultiplexer redis) : IPresenceService
    {
        private readonly IDatabase _db = redis.GetDatabase();

        // Redis Key Prefix
        private static string GetKey(Guid userId) => $"presence:{userId}";
        private readonly TimeSpan _expiry = TimeSpan.FromSeconds(60);

        public async Task HeartbeatAsync(Guid userId)
        {
            var key = GetKey(userId);
            await _db.StringSetAsync(key, "1", _expiry);
        }

        public async Task<List<Guid>> GetOnlineUsersAsync(IEnumerable<Guid> userIds)
        {
            var distinctIds = userIds.Distinct().ToList();
            
            if (distinctIds.Count == 0) 
                return [];

            var keys = distinctIds
                .Select(id => (RedisKey)GetKey(id))
                .ToArray();

            var values = await _db.StringGetAsync(keys);

            var onlineUsers = new List<Guid>();

            for (int i = 0; i < distinctIds.Count; i++)
            {
                if (!values[i].IsNullOrEmpty)
                {
                    onlineUsers.Add(distinctIds[i]);
                }
            }

            return onlineUsers;
        }
    }
}