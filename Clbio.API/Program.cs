using Amazon.S3;
using Clbio.API.DependencyInjection;
using Clbio.API.Extensions;
using Clbio.API.Hubs;
using Clbio.API.Middleware;
using Clbio.Application.Extensions;
using Clbio.Infrastructure.Options;
using Microsoft.Extensions.Options;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args)
    .ConfigureBuilder()
    .ConfigureBuilderServices();

builder.Services.AddHealthChecks();

var awsSettings = new AwsSettings
{
    BucketName = Environment.GetEnvironmentVariable("AwsSettings__BucketName")?.Trim() ?? "",
    Region = Environment.GetEnvironmentVariable("AwsSettings__Region")?.Trim() ?? "eu-central-1",
    AccessKey = Environment.GetEnvironmentVariable("AwsSettings__AccessKey")?.Trim() ?? "",
    SecretKey = Environment.GetEnvironmentVariable("AwsSettings__SecretKey")?.Trim() ?? "",
    PublicUrl = Environment.GetEnvironmentVariable("AwsSettings__PublicUrl")?.Trim()
};

Console.WriteLine($"#---[FINAL-DEBUG]---# Bucket: '{awsSettings.BucketName}'");
Console.WriteLine($"#---[FINAL-DEBUG]---# Region: '{awsSettings.Region}'");

if (string.IsNullOrEmpty(awsSettings.BucketName))
{
    throw new Exception("ERROR: AwsSettings__BucketName not found!");
}

builder.Services.AddSingleton(Options.Create(awsSettings));

builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var credentials = new Amazon.Runtime.BasicAWSCredentials(awsSettings.AccessKey, awsSettings.SecretKey);
    var config = new AmazonS3Config
    {
        RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(awsSettings.Region)
    };
    return new AmazonS3Client(credentials, config);
});


var app = builder.Build();

SafeExecution.IsDevelopment = app.Environment.IsDevelopment();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error"); 
}

app.ApplyMigrations();
await app.AddRolePermissionSeederAsync();

app.UseMiddleware<ErrorHandlerMiddleware>();
app.UseApiSecurity(app.Environment);
app.UseRouting();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseCors("AllowFrontendDev");
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHub<AppHub>("/hubs/app");

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