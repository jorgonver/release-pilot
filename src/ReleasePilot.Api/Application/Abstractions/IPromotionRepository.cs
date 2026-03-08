using ReleasePilot.Api.Domain.Promotions;

namespace ReleasePilot.Api.Application.Abstractions;

public interface IPromotionRepository
{
    Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Promotion>> ListAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Promotion>> ListByApplicationAsync(string applicationName, CancellationToken cancellationToken);

    Task AddAsync(Promotion promotion, CancellationToken cancellationToken);

    Task UpdateAsync(Promotion promotion, CancellationToken cancellationToken);
}
