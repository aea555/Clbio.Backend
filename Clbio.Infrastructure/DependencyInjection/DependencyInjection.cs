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
                .AddRepositories(configuration)
                .AddUnitOfWork(configuration);

            return services;
        }
    }
}
