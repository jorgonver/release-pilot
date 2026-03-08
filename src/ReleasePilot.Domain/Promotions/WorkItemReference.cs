using ReleasePilot.Api.Domain.Primitives;

namespace ReleasePilot.Api.Domain.Promotions;

public sealed class WorkItemReference
{
    public WorkItemReference(string externalId, string? title = null)
    {
        if (string.IsNullOrWhiteSpace(externalId))
        {
            throw new DomainRuleViolationException("Work item external id is required.");
        }

        ExternalId = externalId.Trim();
        Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
    }

    public string ExternalId { get; }

    public string? Title { get; }
}
