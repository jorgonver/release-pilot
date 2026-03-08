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

        services.AddScoped<ICommandHandler<RequestPromotionCommand, PromotionDto>, RequestPromotionCommandHandler>();
        services.AddScoped<ICommandHandler<ApprovePromotionCommand, PromotionDto>, ApprovePromotionCommandHandler>();
        services.AddScoped<ICommandHandler<StartDeploymentCommand, PromotionDto>, StartDeploymentCommandHandler>();
        services.AddScoped<ICommandHandler<CompletePromotionCommand, PromotionDto>, CompletePromotionCommandHandler>();
        services.AddScoped<ICommandHandler<RollbackPromotionCommand, PromotionDto>, RollbackPromotionCommandHandler>();
        services.AddScoped<ICommandHandler<CancelPromotionCommand, PromotionDto>, CancelPromotionCommandHandler>();

        services.AddScoped<IQueryHandler<ListPromotionsQuery, IReadOnlyCollection<PromotionDto>>, ListPromotionsQueryHandler>();
        services.AddScoped<IQueryHandler<ListApplicationsQuery, IReadOnlyCollection<string>>, ListApplicationsQueryHandler>();
        services.AddScoped<IQueryHandler<GetPromotionByIdQuery, PromotionDto?>, GetPromotionByIdQueryHandler>();
        services.AddScoped<IQueryHandler<ListPromotionsByApplicationQuery, PaginatedPromotionsResult>, ListPromotionsByApplicationQueryHandler>();
        services.AddScoped<IQueryHandler<GetEnvironmentStatusQuery, EnvironmentStatusResult>, GetEnvironmentStatusQueryHandler>();

        return services;
    }
}
