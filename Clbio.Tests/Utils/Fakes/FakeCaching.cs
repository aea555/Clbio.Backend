using Clbio.Abstractions.Interfaces.Cache;
using Clbio.Domain.Enums;

namespace Clbio.Tests.Utils.Fakes
{
    public class FakeCaching : ICacheService
    {
        private readonly Dictionary<string, object?> _store = new();

        public Task<T?> GetAsync<T>(string key)
        {
            _store.TryGetValue(key, out var value);
            return Task.FromResult((T?)value);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            _store[key] = value;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            _store.Remove(key);
            return Task.CompletedTask;
        }

        public Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            if (_store.TryGetValue(key, out var existing))
                return Task.FromResult((T)existing!);

            return factory();
        }

        public Task<List<T?>> GetManyAsync<T>(IEnumerable<string> keys)
        {
            var list = keys
                .Select(k => _store.TryGetValue(k, out var v) ? (T?)v : default)
                .ToList();

            return Task.FromResult((List<T?>)list);
        }

        public Task SetManyAsync<T>(Dictionary<string, T> values, TimeSpan? expiration = null)
        {
            foreach (var kv in values)
                _store[kv.Key] = kv.Value;

            return Task.CompletedTask;
        }
    }


    // =========================================================================
    // Fake ICacheVersionService (per-key numeric counters in memory)
    // =========================================================================
    public class FakeCacheVersionService : ICacheVersionService
    {
        private readonly Dictionary<string, long> _versions = new();

        private long Ensure(string key)
        {
            if (!_versions.TryGetValue(key, out var v))
            {
                v = 1;
                _versions[key] = v;
            }
            return v;
        }

        // ---------------- Workspace Version ----------------
        public Task<long> GetWorkspaceVersionAsync(Guid workspaceId)
            => Task.FromResult(Ensure($"ver:ws:{workspaceId}"));

        public Task<long> BumpWorkspaceVersionAsync(Guid workspaceId)
        {
            var key = $"ver:ws:{workspaceId}";
            var v = Ensure(key) + 1;
            _versions[key] = v;
            return Task.FromResult(v);
        }

        // ---------------- Workspace Role Version ----------------
        public Task<long> GetWorkspaceRoleVersionAsync(WorkspaceRole role)
            => Task.FromResult(Ensure($"ver:wsrole:{role}"));

        public Task<long> BumpWorkspaceRoleVersionAsync(WorkspaceRole role)
        {
            var key = $"ver:wsrole:{role}";
            var v = Ensure(key) + 1;
            _versions[key] = v;
            return Task.FromResult(v);
        }

        // ---------------- Membership Version ----------------
        public Task<long> GetMembershipVersionAsync(Guid userId, Guid workspaceId)
            => Task.FromResult(Ensure($"ver:member:{workspaceId}:{userId}"));

        public Task<long> IncrementMembershipVersionAsync(Guid userId, Guid workspaceId)
        {
            var key = $"ver:member:{workspaceId}:{userId}";
            var v = Ensure(key) + 1;
            _versions[key] = v;
            return Task.FromResult(v);
        }
    }


    // =========================================================================
    // Fake ICacheInvalidationService (no-op)
    // =========================================================================
    public class FakeCacheInvalidationService : ICacheInvalidationService
    {
        public Task InvalidateWorkspace(Guid workspaceId)
            => Task.CompletedTask;

        public Task InvalidateWorkspaceRole(WorkspaceRole role)
            => Task.CompletedTask;

        public Task InvalidateMembership(Guid userId, Guid workspaceId)
            => Task.CompletedTask;

        public Task InvalidateUser(Guid userId)
            => Task.CompletedTask;
    }
}
