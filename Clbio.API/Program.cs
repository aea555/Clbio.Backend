using Clbio.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

string connectionString;

if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
{
    connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_DOCKER");
}
else
{
    connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_LOCAL") ??
                       builder.Configuration.GetConnectionString("DefaultConnection");
}

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

// auto-migrate on start
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

    if (env.IsDevelopment())
    {
        try
        {
            db.Database.Migrate();
            Console.WriteLine("[Development] Database migrated successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Development] Database migration failed: {ex.Message}");
            throw;
        }
    }
    else
    {
        Console.WriteLine("Skipping automatic migrations (Production environment).");
    }
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();

