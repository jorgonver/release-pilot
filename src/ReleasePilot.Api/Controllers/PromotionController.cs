using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Application.Promotions;
using ReleasePilot.Api.Application.Promotions.Commands;
using ReleasePilot.Api.Application.Promotions.Queries;
using ReleasePilot.Api.Dto;
using ReleasePilot.Api.Extensions;

namespace ReleasePilot.Api.Controllers;

[Route("api/promotions")]
[ApiController]
public class PromotionController : ControllerBase
{
    private readonly IRequestDispatcher _dispatcher;

    public PromotionController(IRequestDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpGet]
    [EnableRateLimiting(ApiServiceCollectionExtensions.RateLimitPolicies.PromotionRead)]
    public async Task<IActionResult> ListPromotions(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.SendQueryAsync<ListPromotionsQuery, IReadOnlyCollection<PromotionDto>>(
            new ListPromotionsQuery(),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("applications")]
    [EnableRateLimiting(ApiServiceCollectionExtensions.RateLimitPolicies.PromotionRead)]
    public async Task<IActionResult> ListApplications(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.SendQueryAsync<ListApplicationsQuery, IReadOnlyCollection<string>>(
            new ListApplicationsQuery(),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("applications/{applicationName}")]
    [EnableRateLimiting(ApiServiceCollectionExtensions.RateLimitPolicies.PromotionRead)]
    public async Task<IActionResult> ListByApplication(
        string applicationName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _dispatcher.SendQueryAsync<ListPromotionsByApplicationQuery, PaginatedPromotionsResult>(
            new ListPromotionsByApplicationQuery(applicationName, page, pageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("applications/{applicationName}/environments/status")]
    [EnableRateLimiting(ApiServiceCollectionExtensions.RateLimitPolicies.PromotionRead)]
    public async Task<IActionResult> GetEnvironmentStatus(string applicationName, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.SendQueryAsync<GetEnvironmentStatusQuery, EnvironmentStatusResult>(
            new GetEnvironmentStatusQuery(applicationName),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [EnableRateLimiting(ApiServiceCollectionExtensions.RateLimitPolicies.PromotionRead)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.SendQueryAsync<GetPromotionByIdQuery, PromotionDto?>(
            new GetPromotionByIdQuery(id),
            cancellationToken);
        return result is null
            ? this.NotFoundError($"Promotion '{id}' not found.")
            : Ok(result);
    }

    [HttpPost]
    [EnableRateLimiting(ApiServiceCollectionExtensions.RateLimitPolicies.PromotionWrite)]
    public async Task<IActionResult> RequestPromotion([FromBody] RequestPromotionDto request, CancellationToken cancellationToken)
    {
        var command = new RequestPromotionCommand(
            request.ApplicationName,
            request.Version,
            request.SourceEnvironment,
            request.TargetEnvironment,
            request.ActingUser,
            (request.WorkItems ?? Array.Empty<RequestPromotionWorkItemDto>())
                .Select(item => new RequestPromotionWorkItemInput(item.ExternalId, item.Title))
                .ToArray());

        var created = await _dispatcher.SendCommandAsync<RequestPromotionCommand, PromotionCommandResult>(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("{id:guid}/approve")]
    [EnableRateLimiting(ApiServiceCollectionExtensions.RateLimitPolicies.PromotionTransition)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApprovePromotionDto request, CancellationToken cancellationToken)
    {
        var updated = await _dispatcher.SendCommandAsync<ApprovePromotionCommand, PromotionCommandResult>(
            new ApprovePromotionCommand(id, request.RequestedByRole, request.ActingUser),
            cancellationToken);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/start")]
    [EnableRateLimiting(ApiServiceCollectionExtensions.RateLimitPolicies.PromotionTransition)]
    public async Task<IActionResult> Start(Guid id, [FromBody] ActingUserDto request, CancellationToken cancellationToken)
    {
        var updated = await _dispatcher.SendCommandAsync<StartDeploymentCommand, PromotionCommandResult>(
            new StartDeploymentCommand(id, request.ActingUser),
            cancellationToken);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/complete")]
    [EnableRateLimiting(ApiServiceCollectionExtensions.RateLimitPolicies.PromotionTransition)]
    public async Task<IActionResult> Complete(Guid id, [FromBody] ActingUserDto request, CancellationToken cancellationToken)
    {
        var updated = await _dispatcher.SendCommandAsync<CompletePromotionCommand, PromotionCommandResult>(
            new CompletePromotionCommand(id, request.ActingUser),
            cancellationToken);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/rollback")]
    [EnableRateLimiting(ApiServiceCollectionExtensions.RateLimitPolicies.PromotionTransition)]
    public async Task<IActionResult> Rollback(Guid id, [FromBody] RollbackPromotionDto request, CancellationToken cancellationToken)
    {
        var updated = await _dispatcher.SendCommandAsync<RollbackPromotionCommand, PromotionCommandResult>(
            new RollbackPromotionCommand(id, request.Reason, request.ActingUser),
            cancellationToken);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/cancel")]
    [EnableRateLimiting(ApiServiceCollectionExtensions.RateLimitPolicies.PromotionTransition)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] ActingUserDto request, CancellationToken cancellationToken)
    {
        var updated = await _dispatcher.SendCommandAsync<CancelPromotionCommand, PromotionCommandResult>(
            new CancelPromotionCommand(id, request.ActingUser),
            cancellationToken);
        return Ok(updated);
    }
}
