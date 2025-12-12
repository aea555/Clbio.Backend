using Clbio.API.DependencyInjection;
using Clbio.API.Extensions;
using Clbio.API.Hubs;
using Clbio.API.Middleware;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args)
    .ConfigureBuilder()
    .ConfigureBuilderServices();

var app = builder.Build();

app.ApplyMigrations();
await app.AddRolePermissionSeederAsync();

//security and middlewares
if (!app.Environment.IsEnvironment("Testing"))
    app.UseCors("AllowFrontendDev");

app.UseApiSecurity(app.Environment);
app.UseMiddleware<ErrorHandlerMiddleware>();

app.UseRouting();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseAuthentication();
    app.UseAuthorization();
}
app.UseRateLimiter();

app.MapControllers();
app.MapHub<AppHub>("/hubs/app");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

/*** DEV ENDPOINTS ***/
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    // --- health check --- //
    app.MapGet("/dev/health", () => Results.Ok("Healthy"));

    // --- see env --- //
    app.MapGet("/dev/env", (IWebHostEnvironment env) => Results.Ok(env.EnvironmentName));

    // --- payload size test --- //
    app.MapPost("/dev/payloadtest", async (HttpContext context) =>
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        return Results.Ok(new { length = body.Length });
    });

    // --- error handler test --- //
    app.MapGet("/dev/errortest", () =>
    {
        throw new Exception("Gotta throw here");
    });
}

app.Run();

public partial class Program { }