using ReleasePilot.AuditWorker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
	.AddOptions<AuditWorkerOptions>()
	.Bind(builder.Configuration.GetSection(AuditWorkerOptions.SectionName))
	.Validate(
		options => !string.IsNullOrWhiteSpace(options.Postgres.ConnectionString),
		$"{AuditWorkerOptions.SectionName}:Postgres:ConnectionString must be configured.")
	.ValidateOnStart();
builder.Services.AddSingleton<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddSingleton<IPromotionEventConsumer, RabbitMqPromotionEventConsumer>();
builder.Services.AddHostedService<AuditLogConsumerWorker>();

var host = builder.Build();
host.Run();
