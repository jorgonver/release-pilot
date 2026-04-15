using Microsoft.AspNetCore.Mvc;
using ReleasePilot.Api.Dto;

namespace ReleasePilot.Api.Extensions;

public static class ControllerErrorResponseExtensions
{
    public static BadRequestObjectResult BadRequestError(this ControllerBase controller, string message)
    {
        return controller.BadRequest(CreatePayload(controller, message));
    }

    public static ConflictObjectResult ConflictError(this ControllerBase controller, string message)
    {
        return controller.Conflict(CreatePayload(controller, message));
    }

    public static NotFoundObjectResult NotFoundError(this ControllerBase controller, string message)
    {
        return controller.NotFound(CreatePayload(controller, message));
    }

    public static ObjectResult UnauthorizedError(this ControllerBase controller, string message)
    {
        return controller.StatusCode(StatusCodes.Status401Unauthorized, CreatePayload(controller, message));
    }

    public static ObjectResult ForbiddenError(this ControllerBase controller, string message)
    {
        return controller.StatusCode(StatusCodes.Status403Forbidden, CreatePayload(controller, message));
    }

    private static ApiErrorResponse CreatePayload(ControllerBase controller, string message)
    {
        return ApiErrorResponse.Create(message, controller.HttpContext.TraceIdentifier);
    }
}