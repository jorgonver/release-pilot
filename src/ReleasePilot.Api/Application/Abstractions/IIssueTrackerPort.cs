namespace ReleasePilot.Api.Application.Abstractions;

public interface IIssueTrackerPort
{
    Task<IReadOnlyCollection<IssueTrackerWorkItem>> GetWorkItemsAsync(
        IReadOnlyCollection<string> references,
        CancellationToken cancellationToken);
}

public sealed record IssueTrackerWorkItem(
    string Id,
    string Title,
    string Description,
    string Status);
