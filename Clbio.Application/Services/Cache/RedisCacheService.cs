using Clbio.Abstractions.Interfaces.Cache;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;

namespace Clbio.Application.Services.Cache
{
    public class RedisCacheService(IDistributedCache cache, IConnectionMultiplexer redis) : ICacheService
    {
        private readonly IDistributedCache _cache = cache;
        private readonly IConnectionMultiplexer _redis = redis;

        public async Task<T?> GetAsync<T>(string key)
        {
            var data = await _cache.GetStringAsync(key);
            return data == null ? default : JsonSerializer.Deserialize<T>(data);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            var json = JsonSerializer.Serialize(value);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(30)
            };

            await _cache.SetStringAsync(key, json, options);
        }

        // --------------------------------------------------------------------
        // BATCH GET
        // --------------------------------------------------------------------
        public async Task<List<T?>> GetManyAsync<T>(IEnumerable<string> keys)
        {
            var db = _redis.GetDatabase();
            var redisKeys = keys.Select(k => (RedisKey)k).ToArray();

            var results = await db.StringGetAsync(redisKeys);

            return results
                .Select(r => r.HasValue
                    ? JsonSerializer.Deserialize<T>(r!)
                    : default)
                .ToList();
        }

        // --------------------------------------------------------------------
        // BATCH SET
        // --------------------------------------------------------------------
        public async Task SetManyAsync<T>(Dictionary<string, T> values, TimeSpan? expiration = null)
        {
            var db = _redis.GetDatabase();
            var expiry = expiration ?? TimeSpan.FromMinutes(30);

            var tasks = new List<Task>();

            foreach (var kvp in values)
            {
                var json = JsonSerializer.Serialize(kvp.Value);
                tasks.Add(db.StringSetAsync(kvp.Key, json, expiry));
            }

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
            => _cache.RemoveAsync(key);
    }
}
