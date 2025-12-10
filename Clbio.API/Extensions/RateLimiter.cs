using System.Security.Claims;
using System.Threading.RateLimiting;

namespace Clbio.API.Extensions
{
    public static class RateLimiter
    {
        public static IServiceCollection AddGlobalRateLimiter(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                // return HTTP 429 Too Many Requests 
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // dynamic Global limiter
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    // 1. IS USER AUTHENTICATED?
                    if (context.User.Identity?.IsAuthenticated == true)
                    {
                        // AUTHENTICATED user policy
                        // Partition Key: UserId (from token)
                        // Limit: 100 requests per minute
                        // type: sliding window for smoother transitions

                        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                        // block suspicious user that is somehow authenticated but doesn't actually have an id.
                        if (string.IsNullOrEmpty(userId))
                        {
                            return RateLimitPartition.GetFixedWindowLimiter("invalid_auth", _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 1,
                                Window = TimeSpan.FromDays(1000),
                                QueueLimit = 0
                            });
                        }

                        return RateLimitPartition.GetSlidingWindowLimiter(
                            partitionKey: userId,
                            factory: _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = 100,
                                Window = TimeSpan.FromMinutes(1),
                                SegmentsPerWindow = 2,
                                QueueLimit = 10,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                            });
                    }
                    else
                    {
                        // UNAUTHENTICATED user policy
                        // Partition Key: IP address
                        // Limit: 20 requests per minute
                        // Type: Fixed Window (more rigid)

                        var ip = context.Connection.RemoteIpAddress?.ToString();

                        // block users with an unknown ip address
                        if (string.IsNullOrEmpty(ip))
                        {
                            return RateLimitPartition.GetFixedWindowLimiter("unknown_ip", _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 1,
                                Window = TimeSpan.FromDays(1000),
                                QueueLimit = 0
                            });
                        }

                        return RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey: ip,
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = 20,
                                Window = TimeSpan.FromMinutes(1),
                                QueueLimit = 2,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                            });
                    }
                });
            });

            return services;
        }
    }
}