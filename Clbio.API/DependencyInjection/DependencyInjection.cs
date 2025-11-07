using Clbio.Application.DependencyInjection;
using Clbio.Infrastructure.DependencyInjection;

namespace Clbio.API.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddClbio(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddInfrastructure(configuration)
                .AddApplication(configuration);

            return services;
        }
    }
}
