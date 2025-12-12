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

            if (env.IsDevelopment())
            {
                try
                {
                    db.Database.Migrate();
                    Console.WriteLine("#---[Devmode]---# Database migrated successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"#---[Devmode]---# Database migration failed: {ex.Message}");
                    throw;
                }
            }
            else
            {
                Console.WriteLine("ℹ️ Skipping automatic migrations (Production environment).");
            }
        }
    }
}
