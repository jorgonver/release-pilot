using System.Net.Http.Json;
using ReleasePilot.Api.Application.Abstractions;

namespace ReleasePilot.Api.Infrastructure.Ports;

public sealed class HttpDeploymentPort : IDeploymentPort
{
    private readonly HttpClient _httpClient;

    public HttpDeploymentPort(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task StartDeploymentAsync(DeploymentRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("/deployments/start", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
