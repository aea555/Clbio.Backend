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

app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();

