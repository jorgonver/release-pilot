using System.Net.Http.Json;
using ReleasePilot.Api.Application.Abstractions;

namespace ReleasePilot.Api.Infrastructure.Ports;

public sealed class HttpNotificationPort : INotificationPort
{
    private readonly HttpClient _httpClient;

    public HttpNotificationPort(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task NotifyPromotionTerminalStateAsync(
        PromotionTerminalStateNotification notification,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("/notifications/promotion-terminal-state", notification, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
