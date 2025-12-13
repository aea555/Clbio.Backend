using Clbio.Application.Mappings.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clbio.Application.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAutoMapper(cfg => 
            {
                cfg.AddMaps(typeof(UserMappings).Assembly);
            });
            services
                .AddServices(configuration);

            return services;
        }
    }
}
