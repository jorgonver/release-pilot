using ReleasePilot.Api.Middleware;
using ReleasePilot.Api.Application.Abstractions;

namespace ReleasePilot.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiLayer(this IServiceCollection services)
    {
        services.AddOpenApi();
        services.AddControllers();
        services.AddSingleton<ICorrelationContextAccessor, CorrelationContextAccessor>();

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
        app.MapControllers();

        return app;
    }
}
