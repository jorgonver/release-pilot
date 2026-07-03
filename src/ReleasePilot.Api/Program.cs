using ReleasePilot.Api.Extensions;

namespace ReleasePilot.Api;

public sealed class Program
{
    private Program()
    {
    }

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services
            .AddApiLayer(builder.Configuration)
            .AddApplicationLayer()
            .AddInfrastructureLayer(builder.Configuration);

        var app = builder.Build();

        app.UseApiLayer();

        app.Run();
    }
}

