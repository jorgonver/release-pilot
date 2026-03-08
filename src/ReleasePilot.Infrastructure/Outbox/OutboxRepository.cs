using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using ReleasePilot.Api.Infrastructure.Persistence;

namespace ReleasePilot.Api.Infrastructure.Outbox;

public sealed class OutboxRepository : IOutboxRepository
{
    private readonly PromotionRepositoryOptions _options;

    public OutboxRepository(IOptions<PromotionRepositoryOptions> options)
    {
        _options = options.Value;
    }

    public async Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO outbox_messages (
                id,
                event_type,
                aggregate_id,
                occurred_at,
                payload_json,
                attempt_count,
                processed_at,
                next_attempt_at,
                last_error
            )
            VALUES (
                @Id,
                @EventType,
                @AggregateId,
                @OccurredAt,
                CAST(@PayloadJson AS JSONB),
                @AttemptCount,
                @ProcessedAt,
                @NextAttemptAt,
                @LastError
            );
            """;

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var command = new CommandDefinition(sql, message, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    public async Task<IReadOnlyCollection<OutboxMessage>> GetPendingBatchAsync(int batchSize, int maxRetryCount, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                id,
                event_type AS EventType,
                aggregate_id AS AggregateId,
                occurred_at AS OccurredAt,
                payload_json::text AS PayloadJson,
                attempt_count AS AttemptCount,
                processed_at AS ProcessedAt,
                next_attempt_at AS NextAttemptAt,
                last_error AS LastError
            FROM outbox_messages
            WHERE processed_at IS NULL
              AND attempt_count < @MaxRetryCount
              AND (next_attempt_at IS NULL OR next_attempt_at <= NOW())
            ORDER BY occurred_at
            LIMIT @BatchSize;
            """;

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new { BatchSize = batchSize, MaxRetryCount = maxRetryCount },
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<OutboxMessage>(command);
        return rows.ToArray();
    }

    public async Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE outbox_messages
            SET
                processed_at = NOW(),
                next_attempt_at = NULL,
                last_error = NULL
            WHERE id = @Id;
            """;

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    public async Task MarkFailedAsync(Guid id, string error, int retryDelaySeconds, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE outbox_messages
            SET
                attempt_count = attempt_count + 1,
                last_error = @Error,
                next_attempt_at = NOW() + make_interval(secs => @RetryDelaySeconds)
            WHERE id = @Id;
            """;

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var command = new CommandDefinition(
            sql,
            new { Id = id, Error = error, RetryDelaySeconds = retryDelaySeconds },
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    private NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_options.ConnectionString);
    }
}
