using System.Security.Cryptography;
using System.Text;
using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Dto;

namespace ReleasePilot.Api.Middleware;

public sealed class IdempotencyMiddleware
{
    public const string HeaderName = "Idempotency-Key";

    private readonly RequestDelegate _next;

    public IdempotencyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IIdempotencyStore idempotencyStore)
    {
        if (!ShouldHandle(context))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = context.Request.Headers[HeaderName].ToString().Trim();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        var requestHash = await ComputeRequestHashAsync(context.Request, context.RequestAborted);

        var beginResult = await idempotencyStore.TryBeginAsync(
            new IdempotencyRequest(
                idempotencyKey,
                context.Request.Method,
                path,
                requestHash,
                DateTimeOffset.UtcNow),
            context.RequestAborted);

        if (beginResult.Conflict)
        {
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, "Idempotency key was already used with a different request payload.");
            return;
        }

        if (beginResult.InProgress)
        {
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, "A request with the same idempotency key is already in progress.");
            return;
        }

        if (beginResult.StoredResponse is not null)
        {
            await ReplayStoredResponseAsync(context, beginResult.StoredResponse);
            return;
        }

        if (!beginResult.Started)
        {
            await _next(context);
            return;
        }

        var originalResponseStream = context.Response.Body;
        await using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        try
        {
            await _next(context);

            responseBuffer.Position = 0;
            var responseBody = await new StreamReader(responseBuffer, Encoding.UTF8, leaveOpen: true).ReadToEndAsync(context.RequestAborted);
            responseBuffer.Position = 0;

            if (IsCacheableStatus(context.Response.StatusCode))
            {
                await idempotencyStore.CompleteAsync(
                    new IdempotencyCompletedResponse(
                        idempotencyKey,
                        context.Request.Method,
                        path,
                        context.Response.StatusCode,
                        context.Response.ContentType ?? "application/json",
                        responseBody,
                        DateTimeOffset.UtcNow),
                    context.RequestAborted);
            }
            else
            {
                await idempotencyStore.AbandonAsync(new IdempotencyRequestKey(idempotencyKey, context.Request.Method, path), context.RequestAborted);
            }

            await responseBuffer.CopyToAsync(originalResponseStream, context.RequestAborted);
        }
        catch
        {
            await idempotencyStore.AbandonAsync(new IdempotencyRequestKey(idempotencyKey, context.Request.Method, path), context.RequestAborted);
            throw;
        }
        finally
        {
            context.Response.Body = originalResponseStream;
        }
    }

    private static bool ShouldHandle(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            return false;
        }

        return context.Request.Path.StartsWithSegments("/api/promotions", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCacheableStatus(int statusCode)
    {
        return statusCode is >= 200 and < 300;
    }

    private static async Task<string> ComputeRequestHashAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        request.EnableBuffering();

        string body;
        using (var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
        {
            body = await reader.ReadToEndAsync(cancellationToken);
        }

        request.Body.Position = 0;

        var payload = $"{request.Method}|{request.Path.Value}|{request.QueryString.Value}|{body}";
        var bytes = Encoding.UTF8.GetBytes(payload);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static Task ReplayStoredResponseAsync(HttpContext context, IdempotencyStoredResponse storedResponse)
    {
        context.Response.StatusCode = storedResponse.StatusCode;
        context.Response.ContentType = storedResponse.ContentType;
        context.Response.Headers.Append("X-Idempotency-Replayed", "true");
        return context.Response.WriteAsync(storedResponse.Body, context.RequestAborted);
    }

    private static Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var payload = System.Text.Json.JsonSerializer.Serialize(ApiErrorResponse.Create(message, context.TraceIdentifier));
        return context.Response.WriteAsync(payload, context.RequestAborted);
    }
}
