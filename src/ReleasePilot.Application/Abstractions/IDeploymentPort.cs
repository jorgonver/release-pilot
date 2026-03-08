namespace ReleasePilot.Api.Application.Abstractions;

public interface IDeploymentPort
{
    Task StartDeploymentAsync(DeploymentRequest request, CancellationToken cancellationToken);
}

public sealed record DeploymentRequest(
    Guid PromotionId,
    string ApplicationName,
    string Version,
    string SourceEnvironment,
    string TargetEnvironment);
