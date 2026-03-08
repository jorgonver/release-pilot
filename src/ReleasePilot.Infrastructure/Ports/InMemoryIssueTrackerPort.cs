using ReleasePilot.Api.Application.Abstractions;

namespace ReleasePilot.Api.Infrastructure.Ports;

public sealed class InMemoryIssueTrackerPort : IIssueTrackerPort
{
    public Task<IReadOnlyCollection<IssueTrackerWorkItem>> GetWorkItemsAsync(
        IReadOnlyCollection<string> references,
        CancellationToken cancellationToken)
    {
        var items = references
            .Where(reference => !string.IsNullOrWhiteSpace(reference))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(reference => new IssueTrackerWorkItem(
                reference,
                $"Issue {reference}",
                $"Stub description for issue '{reference}'.",
                "Open"))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<IssueTrackerWorkItem>>(items);
    }
}
