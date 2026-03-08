using System.Collections.Concurrent;
using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Domain.Promotions;

namespace ReleasePilot.Api.Infrastructure.Persistence;

public sealed class InMemoryPromotionRepository : IPromotionRepository
{
    private readonly ConcurrentDictionary<Guid, Promotion> _store = new();

    public Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _store.TryGetValue(id, out var promotion);
        return Task.FromResult(promotion);
    }

    public Task<IReadOnlyCollection<Promotion>> ListAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Promotion> promotions = _store.Values
            .OrderBy(item => item.CreatedAt)
            .ToArray();

        return Task.FromResult(promotions);
    }

    public Task<IReadOnlyCollection<Promotion>> ListByApplicationAsync(string applicationName, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Promotion> promotions = _store.Values
            .Where(item => item.ApplicationName.Equals(applicationName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.CreatedAt)
            .ToArray();

        return Task.FromResult(promotions);
    }

    public Task AddAsync(Promotion promotion, CancellationToken cancellationToken)
    {
        _store[promotion.Id] = promotion;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Promotion promotion, CancellationToken cancellationToken)
    {
        _store[promotion.Id] = promotion;
        return Task.CompletedTask;
    }
}
