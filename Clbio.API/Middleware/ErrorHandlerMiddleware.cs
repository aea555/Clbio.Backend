using System.Net;
using System.Security.Authentication;
using System.Text.Json;

namespace Clbio.API.Middleware
{
    public class ErrorHandlerMiddleware(RequestDelegate next, IHostEnvironment env, ILogger<ErrorHandlerMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<ErrorHandlerMiddleware> _logger = logger;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred");

                context.Response.StatusCode = ex switch
                {
                    AuthenticationException => (int)HttpStatusCode.Unauthorized,
                    ArgumentException => (int)HttpStatusCode.BadRequest,
                    KeyNotFoundException => (int)HttpStatusCode.NotFound,
                    _ => (int)HttpStatusCode.InternalServerError
                };

                context.Response.ContentType = "application/json";
                var errorResponse = new
                {
                    status = context.Response.StatusCode,
                    message = env.IsDevelopment() ? ex.Message: "Unexpected error",
                    traceId = context.TraceIdentifier
                };

                var json = JsonSerializer.Serialize(errorResponse);
                await context.Response.WriteAsync(json);
            }
        }
    }
}
