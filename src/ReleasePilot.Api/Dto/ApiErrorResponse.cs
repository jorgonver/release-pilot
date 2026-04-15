namespace ReleasePilot.Api.Dto;

public sealed record ApiErrorResponse(string Message, string CorrelationId)
{
    public static ApiErrorResponse Create(string message, string correlationId)
    {
        return new ApiErrorResponse(message, correlationId);
    }
}