using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Clbio.Infrastructure.DependencyInjection
{
    public static class RedisDependencyInjection
    {
        public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
        {
            var redisConn = configuration.GetConnectionString("RedisConnection")
                         ?? "localhost:6379";

            // Register the raw Redis connection for:
            //    - pub/sub
            //    - versioning
            //    - key scanning
            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(redisConn));

            // Register the distributed cache API(IDistributedCache)
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConn;
            });

            return services;
        }
    }

}
