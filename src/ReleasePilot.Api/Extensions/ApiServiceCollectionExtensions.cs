using System.Text.Json;
using System.Threading.RateLimiting;
using System.Globalization;
using Microsoft.AspNetCore.RateLimiting;
using ReleasePilot.Api.Middleware;
using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Dto;

namespace ReleasePilot.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiLayer(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection("RateLimiting").Get<RateLimitingSettings>() ?? new RateLimitingSettings();

        services.AddOpenApi();
        services.AddControllers();
        services.AddSingleton<ICorrelationContextAccessor, CorrelationContextAccessor>();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, token) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                context.HttpContext.Response.ContentType = "application/json";

                var correlationId = context.HttpContext.TraceIdentifier;
                var payload = ApiErrorResponse.Create(
                    "Rate limit exceeded. Please retry later.",
                    correlationId);

                await context.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(payload), token);
            };

            options.AddTokenBucketLimiter(RateLimitPolicies.PromotionRead, limiterOptions =>
            {
                limiterOptions.TokenLimit = settings.Read.TokenLimit;
                limiterOptions.TokensPerPeriod = settings.Read.TokensPerPeriod;
                limiterOptions.ReplenishmentPeriod = TimeSpan.FromSeconds(settings.Read.ReplenishmentPeriodSeconds);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = settings.Read.QueueLimit;
                limiterOptions.AutoReplenishment = true;
            });

            options.AddPolicy(RateLimitPolicies.PromotionWrite, httpContext =>
            {
                var partitionKey = ResolvePartitionKey(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.Write.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.Write.WindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = settings.Write.QueueLimit
                    });
            });

            options.AddPolicy(RateLimitPolicies.PromotionTransition, httpContext =>
            {
                var partitionKey = ResolvePartitionKey(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = settings.Transition.PermitLimit,
                        Window = TimeSpan.FromSeconds(settings.Transition.WindowSeconds),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = settings.Transition.QueueLimit
                    });
            });
        });

        return services;
    }

    public static WebApplication UseApiLayer(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<ApiExceptionHandlingMiddleware>();
        app.UseRateLimiter();
        app.MapControllers();

        return app;
    }

    private static string ResolvePartitionKey(HttpContext context)
    {
        var actingUser = context.Request.Headers["X-Acting-User"].ToString();
        if (!string.IsNullOrWhiteSpace(actingUser))
        {
            return actingUser;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    public static class RateLimitPolicies
    {
        public const string PromotionRead = "promotion-read";
        public const string PromotionWrite = "promotion-write";
        public const string PromotionTransition = "promotion-transition";
    }

    public sealed class RateLimitingSettings
    {
        public TokenBucketPolicySettings Read { get; init; } = new();
        public FixedWindowPolicySettings Write { get; init; } = new();
        public FixedWindowPolicySettings Transition { get; init; } = new() { PermitLimit = 5 };
    }

    public sealed class TokenBucketPolicySettings
    {
        public int TokenLimit { get; init; } = 60;
        public int TokensPerPeriod { get; init; } = 60;
        public int ReplenishmentPeriodSeconds { get; init; } = 60;
        public int QueueLimit { get; init; } = 20;
    }

    public sealed class FixedWindowPolicySettings
    {
        public int PermitLimit { get; init; } = 10;
        public int WindowSeconds { get; init; } = 60;
        public int QueueLimit { get; init; }
    }
}
