using Clbio.Abstractions.Interfaces.Cache;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;

namespace Clbio.Application.Services.Cache
{
    public class RedisCacheService(IConnectionMultiplexer redis) : ICacheService
    {
        private readonly IConnectionMultiplexer _redis = redis;
        private IDatabase Db => _redis.GetDatabase();

        public async Task<T?> GetAsync<T>(string key)
        {
            var data = await Db.StringGetAsync(key);
            
            if (!data.HasValue) return default;
            
            return JsonSerializer.Deserialize<T>(data!);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            var json = JsonSerializer.Serialize(value);
            var expiry = expiration ?? TimeSpan.FromMinutes(30);

            await Db.StringSetAsync(key, json, expiry);
        }

        // --------------------------------------------------------------------
        // BATCH GET
        // --------------------------------------------------------------------
        public async Task<List<T?>> GetManyAsync<T>(IEnumerable<string> keys)
        {
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();
            
            var results = await Db.StringGetAsync(redisKeys);

            return results
                .Select(r => r.HasValue ? JsonSerializer.Deserialize<T>(r!) : default)
                .ToList();
        }

        // --------------------------------------------------------------------
        // BATCH SET
        // --------------------------------------------------------------------
        public async Task SetManyAsync<T>(Dictionary<string, T> values, TimeSpan? expiration = null)
        {
            var expiry = expiration ?? TimeSpan.FromMinutes(30);
            
            var batch = Db.CreateBatch();
            var tasks = new List<Task>();

            foreach (var kvp in values)
            {
                var json = JsonSerializer.Serialize(kvp.Value);
                tasks.Add(batch.StringSetAsync(kvp.Key, json, expiry));
            }

            batch.Execute();
            await Task.WhenAll(tasks);
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            var cached = await GetAsync<T>(key);
            if (cached is not null)
                return cached;

            var value = await factory();
            await SetAsync(key, value, expiration);
            return value;
        }

        public Task RemoveAsync(string key)
            => Db.KeyDeleteAsync(key);
    }
}
