using Microsoft.AspNetCore.Mvc;
using ReleasePilot.Api.Dto;

namespace ReleasePilot.Api.Extensions;

public static class ControllerErrorResponseExtensions
{
    public static NotFoundObjectResult NotFoundError(this ControllerBase controller, string message)
    {
        return controller.NotFound(ApiErrorResponse.Create(message, controller.HttpContext.TraceIdentifier));
    }
}