using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Application.Promotions.Commands;
using ReleasePilot.Api.Dto;

namespace ReleasePilot.Api.Tests;

public sealed class IdempotencyIntegrationTests
{
    [Fact]
    public async Task RepeatedPostWithSameIdempotencyKeyReplaysOriginalResponse()
    {
        await using var factory = new IdempotencyTestWebApplicationFactory();
        using var client = factory.CreateClient();

        const string idempotencyKey = "idem-replay-key";

        var payload = new RequestPromotionDto(
            ApplicationName: "checkout-service",
            Version: "1.0.0",
            SourceEnvironment: "dev",
            TargetEnvironment: "staging",
            ActingUser: "api-user",
            WorkItems: Array.Empty<RequestPromotionWorkItemDto>());

        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/promotions")
        {
            Content = JsonContent.Create(payload)
        };
        firstRequest.Headers.Add("Idempotency-Key", idempotencyKey);

        using var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/api/promotions")
        {
            Content = JsonContent.Create(payload)
        };
        secondRequest.Headers.Add("Idempotency-Key", idempotencyKey);

        var firstResponse = await client.SendAsync(firstRequest);
        var secondResponse = await client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        Assert.True(secondResponse.Headers.TryGetValues("X-Idempotency-Replayed", out var replayedValues));
        Assert.Equal("true", Assert.Single(replayedValues));

        var firstId = await ReadIdAsync(firstResponse);
        var secondId = await ReadIdAsync(secondResponse);

        Assert.Equal(firstId, secondId);
        Assert.Equal(1, factory.Dispatcher.CommandCalls);
    }

    [Fact]
    public async Task ReusedKeyWithDifferentPayloadReturnsConflict()
    {
        await using var factory = new IdempotencyTestWebApplicationFactory();
        using var client = factory.CreateClient();

        const string idempotencyKey = "idem-conflict-key";

        var firstPayload = new RequestPromotionDto(
            ApplicationName: "checkout-service",
            Version: "1.0.0",
            SourceEnvironment: "dev",
            TargetEnvironment: "staging",
            ActingUser: "api-user",
            WorkItems: Array.Empty<RequestPromotionWorkItemDto>());

        var secondPayload = firstPayload with { Version = "1.0.1" };

        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/promotions")
        {
            Content = JsonContent.Create(firstPayload)
        };
        firstRequest.Headers.Add("Idempotency-Key", idempotencyKey);

        using var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/api/promotions")
        {
            Content = JsonContent.Create(secondPayload)
        };
        secondRequest.Headers.Add("Idempotency-Key", idempotencyKey);

        var firstResponse = await client.SendAsync(firstRequest);
        var secondResponse = await client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

        var error = await secondResponse.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("Idempotency key was already used with a different request payload.", error.Message);
        Assert.Equal(1, factory.Dispatcher.CommandCalls);
    }

    private static async Task<string> ReadIdAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("id").GetString()!;
    }

    private sealed class IdempotencyTestWebApplicationFactory : WebApplicationFactory<Program>
    {
        public FakeRequestDispatcher Dispatcher { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting(WebHostDefaults.EnvironmentKey, "Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IRequestDispatcher>();
                services.RemoveAll<IIdempotencyStore>();

                services.AddSingleton<IRequestDispatcher>(Dispatcher);
                services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
            });
        }
    }

    private sealed class FakeRequestDispatcher : IRequestDispatcher
    {
        private int _commandCalls;

        public int CommandCalls => _commandCalls;

        public Task<TResponse> SendCommandAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken)
            where TCommand : ICommand<TResponse>
        {
            Interlocked.Increment(ref _commandCalls);

            object response = command switch
            {
                RequestPromotionCommand => new PromotionCommandResult(Guid.Parse("11111111-1111-1111-1111-111111111111")),
                ApprovePromotionCommand approve => new PromotionCommandResult(approve.PromotionId),
                StartDeploymentCommand start => new PromotionCommandResult(start.PromotionId),
                CompletePromotionCommand complete => new PromotionCommandResult(complete.PromotionId),
                RollbackPromotionCommand rollback => new PromotionCommandResult(rollback.PromotionId),
                CancelPromotionCommand cancel => new PromotionCommandResult(cancel.PromotionId),
                _ => throw new InvalidOperationException($"Unsupported command type '{typeof(TCommand).Name}' in test dispatcher.")
            };

            return Task.FromResult((TResponse)response);
        }

        public Task<TResponse> SendQueryAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken)
            where TQuery : IQuery<TResponse>
        {
            throw new NotSupportedException("Query dispatch is not required for idempotency integration tests.");
        }
    }

    private sealed class InMemoryIdempotencyStore : IIdempotencyStore
    {
        private readonly object _sync = new();
        private readonly ConcurrentDictionary<string, Entry> _entries = new();

        public Task<IdempotencyBeginResult> TryBeginAsync(IdempotencyRequest request, CancellationToken cancellationToken)
        {
            lock (_sync)
            {
                var compositeKey = BuildKey(request.Key, request.Method, request.Path);

                if (!_entries.TryGetValue(compositeKey, out var existing))
                {
                    _entries[compositeKey] = new Entry(request.RequestHash, InProgress: true, CompletedResponse: null);
                    return Task.FromResult(new IdempotencyBeginResult(Started: true, InProgress: false, Conflict: false, StoredResponse: null));
                }

                if (!string.Equals(existing.RequestHash, request.RequestHash, StringComparison.Ordinal))
                {
                    return Task.FromResult(new IdempotencyBeginResult(Started: false, InProgress: false, Conflict: true, StoredResponse: null));
                }

                if (existing.CompletedResponse is not null)
                {
                    return Task.FromResult(new IdempotencyBeginResult(Started: false, InProgress: false, Conflict: false, StoredResponse: existing.CompletedResponse));
                }

                return Task.FromResult(new IdempotencyBeginResult(Started: false, InProgress: true, Conflict: false, StoredResponse: null));
            }
        }

        public Task CompleteAsync(IdempotencyCompletedResponse response, CancellationToken cancellationToken)
        {
            lock (_sync)
            {
                var compositeKey = BuildKey(response.Key, response.Method, response.Path);
                _entries[compositeKey] = new Entry(
                    RequestHash: _entries[compositeKey].RequestHash,
                    InProgress: false,
                    CompletedResponse: new IdempotencyStoredResponse(response.StatusCode, response.ContentType, response.Body));
            }

            return Task.CompletedTask;
        }

        public Task AbandonAsync(IdempotencyRequestKey requestKey, CancellationToken cancellationToken)
        {
            lock (_sync)
            {
                var compositeKey = BuildKey(requestKey.Key, requestKey.Method, requestKey.Path);
                _entries.TryRemove(compositeKey, out _);
            }

            return Task.CompletedTask;
        }

        private static string BuildKey(string key, string method, string path)
        {
            return $"{key}|{method}|{path}";
        }

        private sealed record Entry(string RequestHash, bool InProgress, IdempotencyStoredResponse? CompletedResponse);
    }
}
