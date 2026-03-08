using Microsoft.AspNetCore.Mvc;
using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Application.Promotions;
using ReleasePilot.Api.Application.Promotions.Commands;
using ReleasePilot.Api.Application.Promotions.Queries;
using ReleasePilot.Api.Domain.Primitives;

namespace ReleasePilot.Api.Controllers;

[Route("api/promotions")]
[ApiController]
public class PromotionController : ControllerBase
{
    private readonly ICommandHandler<RequestPromotionCommand, PromotionDto> _requestPromotionHandler;
    private readonly ICommandHandler<ApprovePromotionCommand, PromotionDto> _approvePromotionHandler;
    private readonly ICommandHandler<StartDeploymentCommand, PromotionDto> _startDeploymentHandler;
    private readonly ICommandHandler<CompletePromotionCommand, PromotionDto> _completePromotionHandler;
    private readonly ICommandHandler<RollbackPromotionCommand, PromotionDto> _rollbackPromotionHandler;
    private readonly ICommandHandler<CancelPromotionCommand, PromotionDto> _cancelPromotionHandler;
    private readonly IQueryHandler<GetPromotionByIdQuery, PromotionDto?> _getByIdQueryHandler;
    private readonly IQueryHandler<ListPromotionsByApplicationQuery, PaginatedPromotionsResult> _listByApplicationQueryHandler;
    private readonly IQueryHandler<GetEnvironmentStatusQuery, EnvironmentStatusResult> _getEnvironmentStatusQueryHandler;

    public PromotionController(
        ICommandHandler<RequestPromotionCommand, PromotionDto> requestPromotionHandler,
        ICommandHandler<ApprovePromotionCommand, PromotionDto> approvePromotionHandler,
        ICommandHandler<StartDeploymentCommand, PromotionDto> startDeploymentHandler,
        ICommandHandler<CompletePromotionCommand, PromotionDto> completePromotionHandler,
        ICommandHandler<RollbackPromotionCommand, PromotionDto> rollbackPromotionHandler,
        ICommandHandler<CancelPromotionCommand, PromotionDto> cancelPromotionHandler,
        IQueryHandler<GetPromotionByIdQuery, PromotionDto?> getByIdQueryHandler,
        IQueryHandler<ListPromotionsByApplicationQuery, PaginatedPromotionsResult> listByApplicationQueryHandler,
        IQueryHandler<GetEnvironmentStatusQuery, EnvironmentStatusResult> getEnvironmentStatusQueryHandler)
    {
        _requestPromotionHandler = requestPromotionHandler;
        _approvePromotionHandler = approvePromotionHandler;
        _startDeploymentHandler = startDeploymentHandler;
        _completePromotionHandler = completePromotionHandler;
        _rollbackPromotionHandler = rollbackPromotionHandler;
        _cancelPromotionHandler = cancelPromotionHandler;
        _getByIdQueryHandler = getByIdQueryHandler;
        _listByApplicationQueryHandler = listByApplicationQueryHandler;
        _getEnvironmentStatusQueryHandler = getEnvironmentStatusQueryHandler;
    }

    [HttpGet("applications/{applicationName}")]
    public async Task<IActionResult> ListByApplication(
        string applicationName,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _listByApplicationQueryHandler.HandleAsync(
                new ListPromotionsByApplicationQuery(applicationName, page, pageSize),
                cancellationToken);
            return Ok(result);
        }
        catch (DomainRuleViolationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("applications/{applicationName}/environments/status")]
    public async Task<IActionResult> GetEnvironmentStatus(string applicationName, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _getEnvironmentStatusQueryHandler.HandleAsync(
                new GetEnvironmentStatusQuery(applicationName),
                cancellationToken);
            return Ok(result);
        }
        catch (DomainRuleViolationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getByIdQueryHandler.HandleAsync(new GetPromotionByIdQuery(id), cancellationToken);
        return result is null
            ? NotFound(new { message = $"Promotion '{id}' not found." })
            : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> RequestPromotion([FromBody] RequestPromotionHttpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new RequestPromotionCommand(
                request.ApplicationName,
                request.Version,
                request.SourceEnvironment,
                request.TargetEnvironment,
                (request.WorkItems ?? Array.Empty<RequestPromotionWorkItemHttpRequest>())
                    .Select(item => new RequestPromotionWorkItemInput(item.ExternalId, item.Title))
                    .ToArray());

            var created = await _requestPromotionHandler.HandleAsync(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (DomainRuleViolationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApprovePromotionHttpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _approvePromotionHandler.HandleAsync(
                new ApprovePromotionCommand(id, request.RequestedByRole),
                cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (DomainRuleViolationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _startDeploymentHandler.HandleAsync(new StartDeploymentCommand(id), cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (DomainRuleViolationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _completePromotionHandler.HandleAsync(new CompletePromotionCommand(id), cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (DomainRuleViolationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/rollback")]
    public async Task<IActionResult> Rollback(Guid id, [FromBody] RollbackPromotionHttpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _rollbackPromotionHandler.HandleAsync(
                new RollbackPromotionCommand(id, request.Reason),
                cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (DomainRuleViolationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _cancelPromotionHandler.HandleAsync(new CancelPromotionCommand(id), cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (DomainRuleViolationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public sealed record RequestPromotionHttpRequest(
    string ApplicationName,
    string Version,
    string SourceEnvironment,
    string TargetEnvironment,
    IReadOnlyCollection<RequestPromotionWorkItemHttpRequest> WorkItems);

public sealed record RequestPromotionWorkItemHttpRequest(string ExternalId, string? Title);

public sealed record ApprovePromotionHttpRequest(string RequestedByRole);

public sealed record RollbackPromotionHttpRequest(string Reason);
