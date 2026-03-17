using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Domain.Promotions;

namespace ReleasePilot.Api.Infrastructure.Persistence;

public sealed class PromotionRepository : IPromotionRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly PromotionRepositoryOptions _options;

    public PromotionRepository(IOptions<PromotionRepositoryOptions> options)
    {
        _options = options.Value;
    }

    public async Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                id,
                application_name AS ApplicationName,
                version AS Version,
                source_environment AS SourceEnvironment,
                target_environment AS TargetEnvironment,
                status AS Status,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt,
                rolled_back_reason AS RolledBackReason,
                completed_at AS CompletedAt,
                work_items_json AS WorkItemsJson,
                state_history_json AS StateHistoryJson
            FROM promotions
            WHERE id = @Id;
            """;

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<PromotionRecord>(command);
        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyCollection<Promotion>> ListAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                id,
                application_name AS ApplicationName,
                version AS Version,
                source_environment AS SourceEnvironment,
                target_environment AS TargetEnvironment,
                status AS Status,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt,
                rolled_back_reason AS RolledBackReason,
                completed_at AS CompletedAt,
                work_items_json AS WorkItemsJson,
                state_history_json AS StateHistoryJson
            FROM promotions
            ORDER BY created_at;
            """;

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<PromotionRecord>(command);
        return rows.Select(Map).ToArray();
    }

    public async Task<IReadOnlyCollection<Promotion>> ListByApplicationAsync(string applicationName, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                id,
                application_name AS ApplicationName,
                version AS Version,
                source_environment AS SourceEnvironment,
                target_environment AS TargetEnvironment,
                status AS Status,
                created_at AS CreatedAt,
                updated_at AS UpdatedAt,
                rolled_back_reason AS RolledBackReason,
                completed_at AS CompletedAt,
                work_items_json AS WorkItemsJson,
                state_history_json AS StateHistoryJson
            FROM promotions
            WHERE application_name ILIKE @ApplicationName
            ORDER BY created_at;
            """;

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var command = new CommandDefinition(sql, new { ApplicationName = applicationName }, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<PromotionRecord>(command);
        return rows.Select(Map).ToArray();
    }

    public async Task AddAsync(Promotion promotion, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO promotions (
                id,
                application_name,
                version,
                source_environment,
                target_environment,
                status,
                created_at,
                updated_at,
                rolled_back_reason,
                completed_at,
                work_items_json,
                state_history_json
            )
            VALUES (
                @Id,
                @ApplicationName,
                @Version,
                @SourceEnvironment,
                @TargetEnvironment,
                @Status,
                @CreatedAt,
                @UpdatedAt,
                @RolledBackReason,
                @CompletedAt,
                CAST(@WorkItemsJson AS JSONB),
                CAST(@StateHistoryJson AS JSONB)
            );
            """;

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var command = new CommandDefinition(sql, ToParameters(promotion), cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    public async Task UpdateAsync(Promotion promotion, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE promotions
            SET
                application_name = @ApplicationName,
                version = @Version,
                source_environment = @SourceEnvironment,
                target_environment = @TargetEnvironment,
                status = @Status,
                created_at = @CreatedAt,
                updated_at = @UpdatedAt,
                rolled_back_reason = @RolledBackReason,
                completed_at = @CompletedAt,
                work_items_json = CAST(@WorkItemsJson AS JSONB),
                state_history_json = CAST(@StateHistoryJson AS JSONB)
            WHERE id = @Id;
            """;

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var command = new CommandDefinition(sql, ToParameters(promotion), cancellationToken: cancellationToken);
        var affectedRows = await connection.ExecuteAsync(command);

        if (affectedRows == 0)
        {
            throw new KeyNotFoundException($"Promotion '{promotion.Id}' was not found.");
        }
    }

    private NpgsqlConnection CreateConnection()
    {
        return new NpgsqlConnection(_options.ConnectionString);
    }

    private static Promotion Map(PromotionRecord row)
    {
        var workItems = DeserializeWorkItems(row.WorkItemsJson);
        var stateHistory = DeserializeStateHistory(row.StateHistoryJson);

        return Promotion.Rehydrate(
            row.Id,
            row.ApplicationName,
            row.Version,
            row.SourceEnvironment,
            row.TargetEnvironment,
            ParseStatus(row.Status),
            ToUtcOffset(row.CreatedAt),
            ToUtcOffset(row.UpdatedAt),
            row.RolledBackReason,
            row.CompletedAt.HasValue ? ToUtcOffset(row.CompletedAt.Value) : null,
            workItems,
            stateHistory);
    }

    private static DateTimeOffset ToUtcOffset(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();

        return new DateTimeOffset(utc);
    }

    private static PromotionStatus ParseStatus(int value)
    {
        if (!Enum.IsDefined(typeof(PromotionStatus), value))
        {
            throw new InvalidOperationException($"Unknown promotion status value persisted in database: {value}.");
        }

        return (PromotionStatus)value;
    }

    private static WorkItemReference[] DeserializeWorkItems(string json)
    {
        var items = JsonSerializer.Deserialize<IReadOnlyCollection<WorkItemData>>(json, JsonOptions)
            ?? Array.Empty<WorkItemData>();

        return items
            .Select(item => new WorkItemReference(item.ExternalId, item.Title))
            .ToArray();
    }

    private static PromotionStateHistoryEntry[] DeserializeStateHistory(string json)
    {
        var entries = JsonSerializer.Deserialize<IReadOnlyCollection<PromotionStateHistoryData>>(json, JsonOptions)
            ?? Array.Empty<PromotionStateHistoryData>();

        return entries
            .Select(entry => new PromotionStateHistoryEntry(
                entry.FromState is null ? null : ParseStatus(entry.FromState.Value),
                ParseStatus(entry.ToState),
                entry.Command,
                entry.OccurredAt,
                entry.Notes))
            .ToArray();
    }

    private static object ToParameters(Promotion promotion)
    {
        var workItemsJson = JsonSerializer.Serialize(
            promotion.WorkItems.Select(item => new WorkItemData(item.ExternalId, item.Title)),
            JsonOptions);

        var stateHistoryJson = JsonSerializer.Serialize(
            promotion.StateHistory.Select(entry => new PromotionStateHistoryData(
                entry.FromState is null ? null : (int)entry.FromState.Value,
                (int)entry.ToState,
                entry.Command,
                entry.OccurredAt,
                entry.Notes)),
            JsonOptions);

        return new
        {
            promotion.Id,
            promotion.ApplicationName,
            promotion.Version,
            promotion.SourceEnvironment,
            promotion.TargetEnvironment,
            Status = (int)promotion.Status,
            promotion.CreatedAt,
            promotion.UpdatedAt,
            promotion.RolledBackReason,
            promotion.CompletedAt,
            WorkItemsJson = workItemsJson,
            StateHistoryJson = stateHistoryJson
        };
    }

    private sealed record PromotionRecord(
        Guid Id,
        string ApplicationName,
        string Version,
        string SourceEnvironment,
        string TargetEnvironment,
        int Status,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        string? RolledBackReason,
        DateTime? CompletedAt,
        string WorkItemsJson,
        string StateHistoryJson);

    private sealed record WorkItemData(string ExternalId, string? Title);

    private sealed record PromotionStateHistoryData(
        int? FromState,
        int ToState,
        string Command,
        DateTimeOffset OccurredAt,
        string? Notes);
}
