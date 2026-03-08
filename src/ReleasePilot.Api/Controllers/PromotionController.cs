using Microsoft.AspNetCore.Mvc;
using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Application.Promotions;
using ReleasePilot.Api.Application.Promotions.Commands;
using ReleasePilot.Api.Application.Promotions.Queries;
using ReleasePilot.Api.Dto;

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
    public async Task<IActionResult> ListPromotions(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.SendQueryAsync<ListPromotionsQuery, IReadOnlyCollection<PromotionDto>>(
            new ListPromotionsQuery(),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("applications")]
    public async Task<IActionResult> ListApplications(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.SendQueryAsync<ListApplicationsQuery, IReadOnlyCollection<string>>(
            new ListApplicationsQuery(),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("applications/{applicationName}")]
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
    public async Task<IActionResult> GetEnvironmentStatus(string applicationName, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.SendQueryAsync<GetEnvironmentStatusQuery, EnvironmentStatusResult>(
            new GetEnvironmentStatusQuery(applicationName),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.SendQueryAsync<GetPromotionByIdQuery, PromotionDto?>(
            new GetPromotionByIdQuery(id),
            cancellationToken);
        return result is null
            ? NotFound(new { message = $"Promotion '{id}' not found." })
            : Ok(result);
    }

    [HttpPost]
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

        var created = await _dispatcher.SendCommandAsync<RequestPromotionCommand, PromotionDto>(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApprovePromotionDto request, CancellationToken cancellationToken)
    {
        var updated = await _dispatcher.SendCommandAsync<ApprovePromotionCommand, PromotionDto>(
            new ApprovePromotionCommand(id, request.RequestedByRole, request.ActingUser),
            cancellationToken);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, [FromBody] ActingUserDto request, CancellationToken cancellationToken)
    {
        var updated = await _dispatcher.SendCommandAsync<StartDeploymentCommand, PromotionDto>(
            new StartDeploymentCommand(id, request.ActingUser),
            cancellationToken);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] ActingUserDto request, CancellationToken cancellationToken)
    {
        var updated = await _dispatcher.SendCommandAsync<CompletePromotionCommand, PromotionDto>(
            new CompletePromotionCommand(id, request.ActingUser),
            cancellationToken);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/rollback")]
    public async Task<IActionResult> Rollback(Guid id, [FromBody] RollbackPromotionDto request, CancellationToken cancellationToken)
    {
        var updated = await _dispatcher.SendCommandAsync<RollbackPromotionCommand, PromotionDto>(
            new RollbackPromotionCommand(id, request.Reason, request.ActingUser),
            cancellationToken);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] ActingUserDto request, CancellationToken cancellationToken)
    {
        var updated = await _dispatcher.SendCommandAsync<CancelPromotionCommand, PromotionDto>(
            new CancelPromotionCommand(id, request.ActingUser),
            cancellationToken);
        return Ok(updated);
    }
}
