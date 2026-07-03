using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using ReleasePilot.Api.Application.Abstractions;

namespace ReleasePilot.Api.Infrastructure.Persistence;

public sealed class IdempotencyStore : IIdempotencyStore
{
    private readonly PromotionRepositoryOptions _options;

    public IdempotencyStore(IOptions<PromotionRepositoryOptions> options)
    {
        _options = options.Value;
    }

    public async Task<IdempotencyBeginResult> TryBeginAsync(IdempotencyRequest request, CancellationToken cancellationToken)
    {
        const string insertSql = """
            INSERT INTO idempotency_requests (
                idempotency_key,
                request_method,
                request_path,
                request_hash,
                created_at
            )
            VALUES (
                @Key,
                @Method,
                @Path,
                @RequestHash,
                @StartedAtUtc
            )
            ON CONFLICT (idempotency_key, request_method, request_path)
            DO NOTHING;
            """;

        const string selectSql = """
            SELECT
                request_hash AS RequestHash,
                status_code AS StatusCode,
                response_content_type AS ContentType,
                response_body AS Body,
                completed_at AS CompletedAt
            FROM idempotency_requests
            WHERE idempotency_key = @Key
              AND request_method = @Method
              AND request_path = @Path;
            """;

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var insertedRows = await connection.ExecuteAsync(
            new CommandDefinition(insertSql, request, cancellationToken: cancellationToken));

        if (insertedRows > 0)
        {
            return new IdempotencyBeginResult(Started: true, InProgress: false, Conflict: false, StoredResponse: null);
        }

        var existing = await connection.QuerySingleOrDefaultAsync<IdempotencyRow>(
            new CommandDefinition(
                selectSql,
                new { request.Key, request.Method, request.Path },
                cancellationToken: cancellationToken));

        if (existing is null)
        {
            return new IdempotencyBeginResult(Started: false, InProgress: false, Conflict: false, StoredResponse: null);
        }

        if (!string.Equals(existing.RequestHash, request.RequestHash, StringComparison.Ordinal))
        {
            return new IdempotencyBeginResult(Started: false, InProgress: false, Conflict: true, StoredResponse: null);
        }

        if (existing.CompletedAt is not null && existing.StatusCode is not null && !string.IsNullOrWhiteSpace(existing.ContentType))
        {
            return new IdempotencyBeginResult(
                Started: false,
                InProgress: false,
                Conflict: false,
                StoredResponse: new IdempotencyStoredResponse(
                    existing.StatusCode.Value,
                    existing.ContentType,
                    existing.Body ?? string.Empty));
        }

        return new IdempotencyBeginResult(Started: false, InProgress: true, Conflict: false, StoredResponse: null);
    }

    public async Task CompleteAsync(IdempotencyCompletedResponse response, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE idempotency_requests
            SET
                status_code = @StatusCode,
                response_content_type = @ContentType,
                response_body = @Body,
                completed_at = @CompletedAtUtc
            WHERE idempotency_key = @Key
              AND request_method = @Method
              AND request_path = @Path
              AND completed_at IS NULL;
            """;

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, response, cancellationToken: cancellationToken));
    }

    public async Task AbandonAsync(IdempotencyRequestKey requestKey, CancellationToken cancellationToken)
    {
        const string sql = """
            DELETE FROM idempotency_requests
            WHERE idempotency_key = @Key
              AND request_method = @Method
              AND request_path = @Path
              AND completed_at IS NULL;
            """;

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, requestKey, cancellationToken: cancellationToken));
    }

    private NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_options.ConnectionString);
    }

    private sealed record IdempotencyRow(
        string RequestHash,
        int? StatusCode,
        string? ContentType,
        string? Body,
        DateTimeOffset? CompletedAt);
}
