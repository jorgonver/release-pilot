namespace ReleasePilot.Api.Application.Abstractions;

public interface IIdempotencyStore
{
    Task<IdempotencyBeginResult> TryBeginAsync(IdempotencyRequest request, CancellationToken cancellationToken);

    Task CompleteAsync(IdempotencyCompletedResponse response, CancellationToken cancellationToken);

    Task AbandonAsync(IdempotencyRequestKey requestKey, CancellationToken cancellationToken);
}

public sealed record IdempotencyRequestKey(string Key, string Method, string Path);

public sealed record IdempotencyRequest(
    string Key,
    string Method,
    string Path,
    string RequestHash,
    DateTimeOffset StartedAtUtc);

public sealed record IdempotencyCompletedResponse(
    string Key,
    string Method,
    string Path,
    int StatusCode,
    string ContentType,
    string Body,
    DateTimeOffset CompletedAtUtc);

public sealed record IdempotencyStoredResponse(int StatusCode, string ContentType, string Body);

public sealed record IdempotencyBeginResult(bool Started, bool InProgress, bool Conflict, IdempotencyStoredResponse? StoredResponse);
