using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using ReleasePilot.Api.Application.Promotions.Events;

namespace ReleasePilot.AuditWorker;

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly AuditWorkerOptions _options;

    public AuditLogRepository(IOptions<AuditWorkerOptions> options)
    {
        _options = options.Value;
    }

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS audit_log (
                id BIGSERIAL PRIMARY KEY,
                event_id UUID NOT NULL UNIQUE,
                event_type TEXT NOT NULL,
                promotion_id UUID NOT NULL,
                occurred_at TIMESTAMPTZ NOT NULL,
                acting_user TEXT NOT NULL,
                payload_json JSONB NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );
            """;

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    public async Task InsertAsync(PromotionEventMessage message, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO audit_log (event_id, event_type, promotion_id, occurred_at, acting_user, payload_json)
            VALUES (@EventId, @EventType, @PromotionId, @OccurredAt, @ActingUser, CAST(@PayloadJson AS JSONB))
            ON CONFLICT (event_id) DO NOTHING;
            """;

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var args = new
        {
            message.EventId,
            message.EventType,
            message.PromotionId,
            OccurredAt = message.OccurredAt.UtcDateTime,
            message.ActingUser,
            message.PayloadJson
        };

        var command = new CommandDefinition(sql, args, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    private NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_options.Postgres.ConnectionString);
    }
}
