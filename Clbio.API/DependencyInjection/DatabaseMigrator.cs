using Clbio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Clbio.API.DependencyInjection
{
    public static class DatabaseMigrator
    {
        public static void ApplyMigrations(this IHost app)
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

            if (env.IsEnvironment("Testing")) return;

            try
            {
                Console.WriteLine($"#---[{env.EnvironmentName}]---# Checking for pending migrations...");

                db.Database.Migrate();

                Console.WriteLine($"#---[{env.EnvironmentName}]---# Database migrated successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"#---[{env.EnvironmentName}]---# CRITICAL: Migration failed: {ex.Message}");
                throw;
            }
        }
    }
}
