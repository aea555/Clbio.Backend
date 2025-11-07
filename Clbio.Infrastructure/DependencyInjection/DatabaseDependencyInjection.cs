using Clbio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clbio.Infrastructure.DependencyInjection
{
    public static class DatabaseDependencyInjection
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            string connectionString;

            if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
            {
                connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_DOCKER");
            }
            else
            {
                connectionString =
                    Environment.GetEnvironmentVariable("DB_CONNECTION_LOCAL") ??
                    configuration.GetConnectionString("DefaultConnection");
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            return services;
        }
    }
}
