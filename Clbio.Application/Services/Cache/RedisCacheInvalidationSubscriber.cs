using Clbio.Application.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Clbio.Application.Services.Cache
{
    public class RedisCacheInvalidationSubscriber(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheInvalidationSubscriber> logger) : BackgroundService
    {
        private readonly IConnectionMultiplexer _redis = redis;
        private readonly ILogger<RedisCacheInvalidationSubscriber> _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var sub = _redis.GetSubscriber();

            await sub.SubscribeAsync(RedisChannel.Literal(CacheChannels.WorkspaceInvalidated), (channel, value) =>
            {
                _logger.LogInformation("Workspace invalidation received: {WorkspaceId}", value);
                // Todo: clear in-memory caches for the workspace
            });

            await sub.SubscribeAsync(RedisChannel.Literal(CacheChannels.WorkspaceRoleInvalidated), (channel, value) =>
            {
                _logger.LogInformation("Workspace role invalidation received: {Role}", value);
                // todot: clear in-memory role-permission caches
            });

            await sub.SubscribeAsync(RedisChannel.Literal(CacheChannels.UserInvalidated), (channel, value) =>
            {
                _logger.LogInformation("User invalidation received: {UserId}", value);
            });

            await sub.SubscribeAsync(RedisChannel.Literal(CacheChannels.GlobalRoleInvalidated), (channel, value) =>
            {
                _logger.LogInformation("Global role invalidation received: {Role}", value);
                // todot: clear in-memory role-permission caches
            });

            await sub.SubscribeAsync(RedisChannel.Literal(CacheChannels.MembershipInvalidated), (channel, value) =>
            {
                _logger.LogInformation("Membership invalidation received: {Payload}", value);
            });
        }
    }
}
