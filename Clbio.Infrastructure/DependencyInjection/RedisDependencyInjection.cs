using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clbio.Infrastructure.DependencyInjection
{
    public static class RedisDependencyInjection
    {
        public static IServiceCollection AddLocalRedis(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("RedisConnection");
            });
            return services;
        }
    }
}
