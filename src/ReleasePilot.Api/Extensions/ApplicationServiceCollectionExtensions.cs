using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Application.Dispatching;
using ReleasePilot.Api.Application.Promotions;
using ReleasePilot.Api.Application.Promotions.Commands;
using ReleasePilot.Api.Application.Promotions.Queries;

namespace ReleasePilot.Api.Extensions;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        services.AddScoped<IRequestDispatcher, RequestDispatcher>();

        services.AddScoped<ICommandHandler<RequestPromotionCommand, PromotionCommandResult>, RequestPromotionCommandHandler>();
        services.AddScoped<ICommandHandler<ApprovePromotionCommand, PromotionCommandResult>, ApprovePromotionCommandHandler>();
        services.AddScoped<ICommandHandler<StartDeploymentCommand, PromotionCommandResult>, StartDeploymentCommandHandler>();
        services.AddScoped<ICommandHandler<CompletePromotionCommand, PromotionCommandResult>, CompletePromotionCommandHandler>();
        services.AddScoped<ICommandHandler<RollbackPromotionCommand, PromotionCommandResult>, RollbackPromotionCommandHandler>();
        services.AddScoped<ICommandHandler<CancelPromotionCommand, PromotionCommandResult>, CancelPromotionCommandHandler>();

        services.AddScoped<IQueryHandler<ListPromotionsQuery, IReadOnlyCollection<PromotionDto>>, ListPromotionsQueryHandler>();
        services.AddScoped<IQueryHandler<ListApplicationsQuery, IReadOnlyCollection<string>>, ListApplicationsQueryHandler>();
        services.AddScoped<IQueryHandler<GetPromotionByIdQuery, PromotionDto?>, GetPromotionByIdQueryHandler>();
        services.AddScoped<IQueryHandler<ListPromotionsByApplicationQuery, PaginatedPromotionsResult>, ListPromotionsByApplicationQueryHandler>();
        services.AddScoped<IQueryHandler<GetEnvironmentStatusQuery, EnvironmentStatusResult>, GetEnvironmentStatusQueryHandler>();

        return services;
    }
}
