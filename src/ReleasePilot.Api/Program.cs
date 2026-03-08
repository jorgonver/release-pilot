using ReleasePilot.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApiLayer()
    .AddApplicationLayer()
    .AddInfrastructureLayer(builder.Configuration);

var app = builder.Build();

app.UseApiLayer();

app.Run();

