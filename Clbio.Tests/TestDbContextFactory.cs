using Clbio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Clbio.Tests
{
    public static class TestDbContextFactory
    {
        public static AppDbContext CreateContext()
        {
            var connectionString = "Filename=:memory:";

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connectionString)
                .Options;

            var context = new AppDbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();

            return context;
        }

        public static void DestroyContext(AppDbContext context)
        {
            context.Database.CloseConnection();
            context.Database.EnsureDeleted();
            context.Dispose();
        }
    }
}
