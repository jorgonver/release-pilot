var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddScoped<ReleasePilot.Api.Application.Abstractions.IRequestDispatcher, ReleasePilot.Api.Application.Dispatching.RequestDispatcher>();

builder.Services.AddSingleton<ReleasePilot.Api.Application.Abstractions.IPromotionRepository, ReleasePilot.Api.Infrastructure.Persistence.InMemoryPromotionRepository>();
builder.Services.AddScoped<ReleasePilot.Api.Application.Abstractions.IDomainEventDispatcher, ReleasePilot.Api.Infrastructure.Messaging.InMemoryDomainEventDispatcher>();
builder.Services.AddSingleton<ReleasePilot.Api.Application.Abstractions.IDeploymentPort, ReleasePilot.Api.Infrastructure.Ports.NoOpDeploymentPort>();
builder.Services.AddSingleton<ReleasePilot.Api.Application.Abstractions.IIssueTrackerPort, ReleasePilot.Api.Infrastructure.Ports.InMemoryIssueTrackerPort>();
builder.Services.AddSingleton<ReleasePilot.Api.Application.Abstractions.INotificationPort, ReleasePilot.Api.Infrastructure.Ports.InMemoryNotificationPort>();

builder.Services.AddScoped<ReleasePilot.Api.Application.Abstractions.ICommandHandler<ReleasePilot.Api.Application.Promotions.Commands.RequestPromotionCommand, ReleasePilot.Api.Application.Promotions.PromotionDto>, ReleasePilot.Api.Application.Promotions.Commands.RequestPromotionCommandHandler>();
builder.Services.AddScoped<ReleasePilot.Api.Application.Abstractions.ICommandHandler<ReleasePilot.Api.Application.Promotions.Commands.ApprovePromotionCommand, ReleasePilot.Api.Application.Promotions.PromotionDto>, ReleasePilot.Api.Application.Promotions.Commands.ApprovePromotionCommandHandler>();
builder.Services.AddScoped<ReleasePilot.Api.Application.Abstractions.ICommandHandler<ReleasePilot.Api.Application.Promotions.Commands.StartDeploymentCommand, ReleasePilot.Api.Application.Promotions.PromotionDto>, ReleasePilot.Api.Application.Promotions.Commands.StartDeploymentCommandHandler>();
builder.Services.AddScoped<ReleasePilot.Api.Application.Abstractions.ICommandHandler<ReleasePilot.Api.Application.Promotions.Commands.CompletePromotionCommand, ReleasePilot.Api.Application.Promotions.PromotionDto>, ReleasePilot.Api.Application.Promotions.Commands.CompletePromotionCommandHandler>();
builder.Services.AddScoped<ReleasePilot.Api.Application.Abstractions.ICommandHandler<ReleasePilot.Api.Application.Promotions.Commands.RollbackPromotionCommand, ReleasePilot.Api.Application.Promotions.PromotionDto>, ReleasePilot.Api.Application.Promotions.Commands.RollbackPromotionCommandHandler>();
builder.Services.AddScoped<ReleasePilot.Api.Application.Abstractions.ICommandHandler<ReleasePilot.Api.Application.Promotions.Commands.CancelPromotionCommand, ReleasePilot.Api.Application.Promotions.PromotionDto>, ReleasePilot.Api.Application.Promotions.Commands.CancelPromotionCommandHandler>();
builder.Services.AddScoped<ReleasePilot.Api.Application.Abstractions.IQueryHandler<ReleasePilot.Api.Application.Promotions.Queries.ListPromotionsQuery, IReadOnlyCollection<ReleasePilot.Api.Application.Promotions.PromotionDto>>, ReleasePilot.Api.Application.Promotions.Queries.ListPromotionsQueryHandler>();
builder.Services.AddScoped<ReleasePilot.Api.Application.Abstractions.IQueryHandler<ReleasePilot.Api.Application.Promotions.Queries.ListApplicationsQuery, IReadOnlyCollection<string>>, ReleasePilot.Api.Application.Promotions.Queries.ListApplicationsQueryHandler>();
builder.Services.AddScoped<ReleasePilot.Api.Application.Abstractions.IQueryHandler<ReleasePilot.Api.Application.Promotions.Queries.GetPromotionByIdQuery, ReleasePilot.Api.Application.Promotions.PromotionDto?>, ReleasePilot.Api.Application.Promotions.Queries.GetPromotionByIdQueryHandler>();
builder.Services.AddScoped<ReleasePilot.Api.Application.Abstractions.IQueryHandler<ReleasePilot.Api.Application.Promotions.Queries.ListPromotionsByApplicationQuery, ReleasePilot.Api.Application.Promotions.Queries.PaginatedPromotionsResult>, ReleasePilot.Api.Application.Promotions.Queries.ListPromotionsByApplicationQueryHandler>();
builder.Services.AddScoped<ReleasePilot.Api.Application.Abstractions.IQueryHandler<ReleasePilot.Api.Application.Promotions.Queries.GetEnvironmentStatusQuery, ReleasePilot.Api.Application.Promotions.Queries.EnvironmentStatusResult>, ReleasePilot.Api.Application.Promotions.Queries.GetEnvironmentStatusQueryHandler>();
builder.Services.AddScoped<ReleasePilot.Api.Domain.Promotions.Events.PromotionLifecycleLoggingEventHandler>();
builder.Services.AddScoped<ReleasePilot.Api.Application.Promotions.Events.PromotionTerminalStateNotificationHandler>();
builder.Services.AddScoped<ReleasePilot.Api.Application.Abstractions.IDomainEventHandler<ReleasePilot.Api.Domain.Promotions.Events.PromotionRequestedDomainEvent>>(sp => sp.GetRequiredService<ReleasePilot.Api.Domain.Promotions.Events.PromotionLifecycleLoggingEventHandler>());
builder.Services.AddScoped<ReleasePilot.Api.Application.Abstractions.IDomainEventHandler<ReleasePilot.Api.Domain.Promotions.Events.PromotionApprovedDomainEvent>>(sp => sp.GetRequiredService<ReleasePilot.Api.Domain.Promotions.Events.PromotionLifecycleLoggingEventHandler>());
builder.Services.AddScoped<ReleasePilot.Api.Application.Abstractions.IDomainEventHandler<ReleasePilot.Api.Domain.Promotions.Events.DeploymentStartedDomainEvent>>(sp => sp.GetRequiredService<ReleasePilot.Api.Domain.Promotions.Events.PromotionLifecycleLoggingEventHandler>());
builder.Services.AddScoped<ReleasePilot.Api.Application.Abstractions.IDomainEventHandler<ReleasePilot.Api.Domain.Promotions.Events.PromotionCompletedDomainEvent>>(sp => sp.GetRequiredService<ReleasePilot.Api.Domain.Promotions.Events.PromotionLifecycleLoggingEventHandler>());
builder.Services.AddScoped<ReleasePilot.Api.Application.Abstractions.IDomainEventHandler<ReleasePilot.Api.Domain.Promotions.Events.PromotionRolledBackDomainEvent>>(sp => sp.GetRequiredService<ReleasePilot.Api.Domain.Promotions.Events.PromotionLifecycleLoggingEventHandler>());
builder.Services.AddScoped<ReleasePilot.Api.Application.Abstractions.IDomainEventHandler<ReleasePilot.Api.Domain.Promotions.Events.PromotionCancelledDomainEvent>>(sp => sp.GetRequiredService<ReleasePilot.Api.Domain.Promotions.Events.PromotionLifecycleLoggingEventHandler>());
builder.Services.AddScoped<ReleasePilot.Api.Application.Abstractions.IDomainEventHandler<ReleasePilot.Api.Domain.Promotions.Events.PromotionCompletedDomainEvent>>(sp => sp.GetRequiredService<ReleasePilot.Api.Application.Promotions.Events.PromotionTerminalStateNotificationHandler>());
builder.Services.AddScoped<ReleasePilot.Api.Application.Abstractions.IDomainEventHandler<ReleasePilot.Api.Domain.Promotions.Events.PromotionRolledBackDomainEvent>>(sp => sp.GetRequiredService<ReleasePilot.Api.Application.Promotions.Events.PromotionTerminalStateNotificationHandler>());
builder.Services.AddScoped<ReleasePilot.Api.Application.Abstractions.IDomainEventHandler<ReleasePilot.Api.Domain.Promotions.Events.PromotionCancelledDomainEvent>>(sp => sp.GetRequiredService<ReleasePilot.Api.Application.Promotions.Events.PromotionTerminalStateNotificationHandler>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseMiddleware<ReleasePilot.Api.Middleware.ApiExceptionHandlingMiddleware>();

app.MapControllers();

app.Run();

