using Clbio.Abstractions.Interfaces.Cache;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Clbio.Application.Services.Cache
{
    public class RedisCacheService(IDistributedCache cache) : ICacheService
    {
        private readonly IDistributedCache _cache = cache;

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
