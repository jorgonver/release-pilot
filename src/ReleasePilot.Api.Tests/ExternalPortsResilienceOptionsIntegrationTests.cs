using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Infrastructure.Ports;

namespace ReleasePilot.Api.Tests;

public sealed class ExternalPortsResilienceOptionsIntegrationTests
{
    [Fact]
    public void StubModeUsesStubPortImplementations()
    {
        using var factory = CreateFactory(CreateModeOverrides("Stub"));
        var (deploymentPort, issueTrackerPort, notificationPort) = ResolvePorts(factory);

        Assert.IsType<NoOpDeploymentPort>(deploymentPort);
        Assert.IsType<InMemoryIssueTrackerPort>(issueTrackerPort);
        Assert.IsType<InMemoryNotificationPort>(notificationPort);
    }

    [Fact]
    public void HttpModeUsesHttpPortImplementations()
    {
        using var factory = CreateFactory(CreateModeOverrides("Http"));
        var (deploymentPort, issueTrackerPort, notificationPort) = ResolvePorts(factory);

        Assert.IsType<HttpDeploymentPort>(deploymentPort);
        Assert.IsType<HttpIssueTrackerPort>(issueTrackerPort);
        Assert.IsType<HttpNotificationPort>(notificationPort);
    }

    [Fact]
    public void ResilienceOptionsAreBoundFromConfiguration()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>(CreateModeOverrides("Http"))
        {
            ["ExternalPorts:Deployment:TimeoutSeconds"] = "7",
            ["ExternalPorts:Deployment:Resilience:RetryMaxAttempts"] = "4",
            ["ExternalPorts:Deployment:Resilience:CircuitBreakerMinimumThroughput"] = "9",
            ["ExternalPorts:Deployment:Resilience:CircuitBreakerSamplingSeconds"] = "40",
            ["ExternalPorts:Deployment:Resilience:CircuitBreakerBreakSeconds"] = "25",
            ["ExternalPorts:Deployment:Resilience:CircuitBreakerFailureRatio"] = "0.4",
            ["ExternalPorts:Deployment:Resilience:AttemptTimeoutSeconds"] = "6",
            ["ExternalPorts:Deployment:Resilience:TotalTimeoutSeconds"] = "14"
        });

        var options = ResolveExternalPortsOptions(factory);

        Assert.True(options.UseHttpMode);
        Assert.Equal(7, options.Deployment.TimeoutSeconds);
        Assert.Equal(4, options.Deployment.Resilience.RetryMaxAttempts);
        Assert.Equal(9, options.Deployment.Resilience.CircuitBreakerMinimumThroughput);
        Assert.Equal(40, options.Deployment.Resilience.CircuitBreakerSamplingSeconds);
        Assert.Equal(25, options.Deployment.Resilience.CircuitBreakerBreakSeconds);
        Assert.Equal(0.4, options.Deployment.Resilience.CircuitBreakerFailureRatio, 3);
        Assert.Equal(6, options.Deployment.Resilience.AttemptTimeoutSeconds);
        Assert.Equal(14, options.Deployment.Resilience.TotalTimeoutSeconds);
    }

    [Fact]
    public async Task DeploymentHttpPortRetriesTransientFailuresThenSucceeds()
    {
        using var factory = CreateFactory(
            new Dictionary<string, string?>(CreateModeOverrides("Http"))
            {
                ["ExternalPorts:Deployment:Resilience:RetryMaxAttempts"] = "2"
            },
            services =>
            {
                services.AddSingleton(new RetryProbeState(failuresBeforeSuccess: 2));
                services.ConfigureHttpClientDefaults(builder =>
                {
                    builder.ConfigurePrimaryHttpMessageHandler(sp =>
                        new RetryProbeMessageHandler(sp.GetRequiredService<RetryProbeState>()));
                });
            });

        using var scope = factory.Services.CreateScope();

        var deploymentPort = scope.ServiceProvider.GetRequiredService<IDeploymentPort>();
        var probeState = scope.ServiceProvider.GetRequiredService<RetryProbeState>();

        await deploymentPort.StartDeploymentAsync(
            new DeploymentRequest(
                Guid.NewGuid(),
                "checkout-service",
                "1.0.0",
                "dev",
                "staging"),
            CancellationToken.None);

        Assert.Equal(3, probeState.Attempts);
    }

    private static ExternalPortsOptions ResolveExternalPortsOptions(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IOptions<ExternalPortsOptions>>().Value;
    }

    private static (IDeploymentPort DeploymentPort, IIssueTrackerPort IssueTrackerPort, INotificationPort NotificationPort) ResolvePorts(
        WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        return (
            scope.ServiceProvider.GetRequiredService<IDeploymentPort>(),
            scope.ServiceProvider.GetRequiredService<IIssueTrackerPort>(),
            scope.ServiceProvider.GetRequiredService<INotificationPort>());
    }

    private static IReadOnlyDictionary<string, string?> CreateModeOverrides(string mode)
    {
        return new Dictionary<string, string?>
        {
            ["ExternalPorts:Mode"] = mode,
            ["ExternalPorts:Deployment:BaseUrl"] = "https://deployment.test.local",
            ["ExternalPorts:IssueTracker:BaseUrl"] = "https://issues.test.local",
            ["ExternalPorts:Notification:BaseUrl"] = "https://notify.test.local"
        };
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IReadOnlyDictionary<string, string?> overrides,
        Action<IServiceCollection>? configureServices = null)
    {
        return new ExternalPortsTestWebApplicationFactory(overrides, configureServices);
    }

    private sealed class ExternalPortsTestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly IReadOnlyDictionary<string, string?> _overrides;
        private readonly Action<IServiceCollection>? _configureServices;

        public ExternalPortsTestWebApplicationFactory(
            IReadOnlyDictionary<string, string?> overrides,
            Action<IServiceCollection>? configureServices)
        {
            _overrides = overrides;
            _configureServices = configureServices;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting(WebHostDefaults.EnvironmentKey, "Development");
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(_overrides);
            });
            builder.ConfigureServices(services => _configureServices?.Invoke(services));
        }
    }

    private sealed class RetryProbeState
    {
        private int _attempts;

        public RetryProbeState(int failuresBeforeSuccess)
        {
            FailuresBeforeSuccess = failuresBeforeSuccess;
        }

        public int FailuresBeforeSuccess { get; }

        public int Attempts => _attempts;

        public int IncrementAttempt()
        {
            return Interlocked.Increment(ref _attempts);
        }
    }

    private sealed class RetryProbeMessageHandler : HttpMessageHandler
    {
        private readonly RetryProbeState _state;

        public RetryProbeMessageHandler(RetryProbeState state)
        {
            _state = state;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var attempt = _state.IncrementAttempt();
            if (attempt <= _state.FailuresBeforeSuccess)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
