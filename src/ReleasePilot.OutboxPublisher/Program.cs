using ReleasePilot.OutboxPublisher.Extensions;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddOutboxPublisherLayer(builder.Configuration);

var host = builder.Build();
host.Run();
