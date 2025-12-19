namespace Clbio.Abstractions.Interfaces.Cache
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);

        Task<List<T?>> GetManyAsync<T>(IEnumerable<string> keys);
        Task SetManyAsync<T>(Dictionary<string, T> values, TimeSpan? expiration = null);

        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
        Task RemoveAsync(string key);
        Task RemoveAllAsync(IEnumerable<string> keys);
    }
}
