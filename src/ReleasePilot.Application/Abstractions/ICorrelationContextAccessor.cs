namespace ReleasePilot.Api.Application.Abstractions;

public interface ICorrelationContextAccessor
{
    string? CorrelationId { get; set; }
}