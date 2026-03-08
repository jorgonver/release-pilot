using ReleasePilot.Api.Middleware;

namespace ReleasePilot.Api.Extensions;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiLayer(this IServiceCollection services)
    {
        services.AddOpenApi();
        services.AddControllers();

        return services;
    }

    public static WebApplication UseApiLayer(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseMiddleware<ApiExceptionHandlingMiddleware>();
        app.MapControllers();

        return app;
    }
}
