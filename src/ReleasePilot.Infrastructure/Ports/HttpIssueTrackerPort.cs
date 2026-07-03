using System.Net.Http.Json;
using ReleasePilot.Api.Application.Abstractions;

namespace ReleasePilot.Api.Infrastructure.Ports;

public sealed class HttpIssueTrackerPort : IIssueTrackerPort
{
    private readonly HttpClient _httpClient;

    public HttpIssueTrackerPort(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<IssueTrackerWorkItem>> GetWorkItemsAsync(
        IReadOnlyCollection<string> references,
        CancellationToken cancellationToken)
    {
        var normalizedReferences = references
            .Where(reference => !string.IsNullOrWhiteSpace(reference))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedReferences.Length == 0)
        {
            return Array.Empty<IssueTrackerWorkItem>();
        }

        var response = await _httpClient.PostAsJsonAsync(
            "/work-items/resolve",
            new ResolveWorkItemsRequest(normalizedReferences),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<IssueTrackerWorkItem>>(cancellationToken);
        return items ?? Array.Empty<IssueTrackerWorkItem>();
    }

    private sealed record ResolveWorkItemsRequest(IReadOnlyCollection<string> References);
}
