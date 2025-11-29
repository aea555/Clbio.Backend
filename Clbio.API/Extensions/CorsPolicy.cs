namespace Clbio.API.Extensions
{
    public static class CorsPolicy
    {
        public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
        {
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Testing")
            {
                services.AddCors(options =>
                {
                    options.AddPolicy("AllowFrontendDev", policy =>
                        policy.AllowAnyOrigin()
                              .AllowAnyHeader()
                              .AllowAnyMethod());
                });
            }
            else
            {
                services.AddCors(options =>
                {
                    options.AddPolicy("AllowFrontendDev", policy =>
                        policy.WithOrigins("http://localhost:3000")
                              .AllowAnyHeader()
                              .AllowAnyMethod());
                });
            }
            return services;
        }
    }
}
