using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clbio.Infrastructure.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddDatabase(configuration)
                .AddTokenService(configuration)
                .AddRepositories(configuration)
                .AddUnitOfWork(configuration)
                .AddEmailSender(configuration)
                .AddRedis(configuration);

            return services;
        }
    }
}
