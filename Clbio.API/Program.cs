using Clbio.API.DependencyInjection;
using Clbio.API.Extensions;
using Clbio.API.Middleware;

var builder = WebApplication.CreateBuilder(args)
    .ConfigureBuilder()
    .ConfigureBuilderServices();

var app = builder.Build();

app.ApplyMigrations();

//security and middlewares
app.UseCors("AllowFrontendDev");
app.UseRateLimiter();
app.UseApiSecurity(app.Environment);
app.UseMiddleware<ErrorHandlerMiddleware>();

//app.UseAuthentication();             
//app.UseAuthorization();

app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

/*** DEV ENDPOINTS ***/
// --- health check --- //
app.MapGet("/dev/health", () => Results.Ok("Healthy"));

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

app.Run();

public partial class Program { }