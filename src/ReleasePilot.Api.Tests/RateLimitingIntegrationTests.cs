using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using ReleasePilot.Api.Dto;

namespace ReleasePilot.Api.Tests;

public sealed class RateLimitingIntegrationTests
{
    [Fact]
    public async Task GetEndpointsAreLimitedByPromotionReadPolicy()
    {
        await using var factory = new RateLimitTestWebApplicationFactory();
        using var client = factory.CreateClient();

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 120)
                .Select(_ => client.GetAsync("/api/promotions")));

        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task TransitionEndpointsReturn429AfterThreshold()
    {
        await using var factory = new RateLimitTestWebApplicationFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Acting-User", "transition-threshold-user");

        var payload = new { requestedByRole = "Approver", actingUser = "transition-threshold-user" };
        var responses = await Task.WhenAll(
            Enumerable.Range(0, 12)
                .Select(_ =>
                {
                    var promotionId = Guid.NewGuid();
                    return client.PostAsJsonAsync($"/api/promotions/{promotionId}/approve", payload);
                }));

        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task RejectedRequestsContainCorrelationIdAndRetryAfter()
    {
        await using var factory = new RateLimitTestWebApplicationFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Add("X-Acting-User", "retry-header-user");

        var payload = new { requestedByRole = "Approver", actingUser = "retry-header-user" };
        var responses = await Task.WhenAll(
            Enumerable.Range(0, 12)
                .Select(_ =>
                {
                    var promotionId = Guid.NewGuid();
                    return client.PostAsJsonAsync($"/api/promotions/{promotionId}/approve", payload);
                }));

        var rejected = responses.First(response => response.StatusCode == HttpStatusCode.TooManyRequests);

        Assert.True(rejected.Headers.TryGetValues("Retry-After", out var retryAfterValues));

        var retryAfter = Assert.Single(retryAfterValues);
        Assert.True(int.TryParse(retryAfter, out var retryAfterSeconds));
        Assert.True(retryAfterSeconds > 0);

        Assert.True(rejected.Headers.TryGetValues("X-Correlation-Id", out var correlationHeaderValues));
        var correlationHeader = Assert.Single(correlationHeaderValues);

        var error = await rejected.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("Rate limit exceeded. Please retry later.", error.Message);
        Assert.False(string.IsNullOrWhiteSpace(error.CorrelationId));
        Assert.Equal(correlationHeader, error.CorrelationId);
    }

    private sealed class RateLimitTestWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting(WebHostDefaults.EnvironmentKey, "Development");
        }
    }
}
