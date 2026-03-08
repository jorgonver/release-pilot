using ReleasePilot.Api.Application.Abstractions;
using ReleasePilot.Api.Application.Dispatching;
using ReleasePilot.Api.Application.Promotions;
using ReleasePilot.Api.Application.Promotions.Commands;
using ReleasePilot.Api.Application.Promotions.Events;
using ReleasePilot.Api.Application.Promotions.Queries;
using ReleasePilot.Api.Domain.Promotions.Events;
using ReleasePilot.Api.Infrastructure.Messaging;
using ReleasePilot.Api.Infrastructure.Persistence;
using ReleasePilot.Api.Infrastructure.Ports;
using ReleasePilot.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddScoped<IRequestDispatcher, RequestDispatcher>();

builder.Services.AddSingleton<IPromotionRepository, InMemoryPromotionRepository>();
builder.Services.AddScoped<IDomainEventDispatcher, InMemoryDomainEventDispatcher>();
builder.Services.AddSingleton<IDeploymentPort, NoOpDeploymentPort>();
builder.Services.AddSingleton<IIssueTrackerPort, InMemoryIssueTrackerPort>();
builder.Services.AddSingleton<INotificationPort, InMemoryNotificationPort>();

builder.Services.AddScoped<ICommandHandler<RequestPromotionCommand, PromotionDto>, RequestPromotionCommandHandler>();
builder.Services.AddScoped<ICommandHandler<ApprovePromotionCommand, PromotionDto>, ApprovePromotionCommandHandler>();
builder.Services.AddScoped<ICommandHandler<StartDeploymentCommand, PromotionDto>, StartDeploymentCommandHandler>();
builder.Services.AddScoped<ICommandHandler<CompletePromotionCommand, PromotionDto>, CompletePromotionCommandHandler>();
builder.Services.AddScoped<ICommandHandler<RollbackPromotionCommand, PromotionDto>, RollbackPromotionCommandHandler>();
builder.Services.AddScoped<ICommandHandler<CancelPromotionCommand, PromotionDto>, CancelPromotionCommandHandler>();
builder.Services.AddScoped<IQueryHandler<ListPromotionsQuery, IReadOnlyCollection<PromotionDto>>, ListPromotionsQueryHandler>();
builder.Services.AddScoped<IQueryHandler<ListApplicationsQuery, IReadOnlyCollection<string>>, ListApplicationsQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetPromotionByIdQuery, PromotionDto?>, GetPromotionByIdQueryHandler>();
builder.Services.AddScoped<IQueryHandler<ListPromotionsByApplicationQuery, PaginatedPromotionsResult>, ListPromotionsByApplicationQueryHandler>();
builder.Services.AddScoped<IQueryHandler<GetEnvironmentStatusQuery, EnvironmentStatusResult>, GetEnvironmentStatusQueryHandler>();
builder.Services.AddScoped<PromotionLifecycleLoggingEventHandler>();
builder.Services.AddScoped<PromotionTerminalStateNotificationHandler>();
builder.Services.AddScoped<IDomainEventHandler<PromotionRequestedDomainEvent>>(sp => sp.GetRequiredService<PromotionLifecycleLoggingEventHandler>());
builder.Services.AddScoped<IDomainEventHandler<PromotionApprovedDomainEvent>>(sp => sp.GetRequiredService<PromotionLifecycleLoggingEventHandler>());
builder.Services.AddScoped<IDomainEventHandler<DeploymentStartedDomainEvent>>(sp => sp.GetRequiredService<PromotionLifecycleLoggingEventHandler>());
builder.Services.AddScoped<IDomainEventHandler<PromotionCompletedDomainEvent>>(sp => sp.GetRequiredService<PromotionLifecycleLoggingEventHandler>());
builder.Services.AddScoped<IDomainEventHandler<PromotionRolledBackDomainEvent>>(sp => sp.GetRequiredService<PromotionLifecycleLoggingEventHandler>());
builder.Services.AddScoped<IDomainEventHandler<PromotionCancelledDomainEvent>>(sp => sp.GetRequiredService<PromotionLifecycleLoggingEventHandler>());
builder.Services.AddScoped<IDomainEventHandler<PromotionCompletedDomainEvent>>(sp => sp.GetRequiredService<PromotionTerminalStateNotificationHandler>());
builder.Services.AddScoped<IDomainEventHandler<PromotionRolledBackDomainEvent>>(sp => sp.GetRequiredService<PromotionTerminalStateNotificationHandler>());
builder.Services.AddScoped<IDomainEventHandler<PromotionCancelledDomainEvent>>(sp => sp.GetRequiredService<PromotionTerminalStateNotificationHandler>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseMiddleware<ApiExceptionHandlingMiddleware>();

app.MapControllers();

app.Run();

