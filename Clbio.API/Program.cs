using Clbio.API.DependencyInjection;
using Clbio.API.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services
    .AddOpenApi()
    .AddClbio(builder.Configuration)
    .AddCorsPolicy()
    .AddGlobalRateLimiter();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok("Healthy"));

app.ApplyMigrations();

app.UseHttpsRedirection();

app.UseCors("AllowFrontendDev");

app.UseRateLimiter();

app.Run();

