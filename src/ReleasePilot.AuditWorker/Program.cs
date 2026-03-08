using ReleasePilot.AuditWorker.Extensions;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddAuditWorkerLayer(builder.Configuration);

var host = builder.Build();
host.Run();
