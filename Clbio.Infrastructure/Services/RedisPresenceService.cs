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
            var onlineUsers = new List<Guid>();

            // batch querying with Redis Pipelining
            var batch = _db.CreateBatch();
            var tasks = new List<Task<bool>>();

            // distinct Ids
            var distinctIds = userIds.Distinct().ToList();

            foreach (var id in distinctIds)
            {
                // add tasks to batch
                tasks.Add(batch.KeyExistsAsync(GetKey(id)));
            }

            // send all
            batch.Execute();

            // gather results
            var results = await Task.WhenAll(tasks);

            for (int i = 0; i < distinctIds.Count; i++)
            {
                if (results[i])
                {
                    onlineUsers.Add(distinctIds[i]);
                }
            }

            return onlineUsers;
        }
    }
}