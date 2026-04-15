using System.Text.Json;
using ReleasePilot.Api.Dto;
using ReleasePilot.Api.Domain.Primitives;

namespace ReleasePilot.Api.Middleware;

public sealed class ApiExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;

    public ApiExceptionHandlingMiddleware(RequestDelegate next, ILogger<ApiExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.TraceIdentifier;

        try
        {
            await _next(context);
        }
        catch (DomainRuleViolationException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, ex.Message, correlationId);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status404NotFound, ex.Message, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled API exception. CorrelationId: {CorrelationId}", correlationId);
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.", correlationId);
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message, string correlationId)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = JsonSerializer.Serialize(ApiErrorResponse.Create(message, correlationId));
        await context.Response.WriteAsync(payload);
    }
}
