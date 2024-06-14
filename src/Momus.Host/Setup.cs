using Microsoft.Extensions.Options;
using Momus.Config;
using Momus.Host.Middleware;
using Momus.Middleware;
using NATS.Extensions.Microsoft.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace Momus.Host;

public static class Setup
{
    internal static void ConfigureBuilder(this WebApplicationBuilder builder)
    {
        builder.AddConfig();
        builder.AddLogging();
        builder.ConfigureNats();
        builder.Services.AddHostedService<RouteConfigBackgroundService>();
    }

    internal static void ConfigureApplication(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseMiddleware<WwwRedirectMiddleware>();
        // need to test this again with and without cloudflare in the middle
        app.UseMiddleware<SchemeForwardingMiddleware>();
    }

    private static void ConfigureNats(this WebApplicationBuilder builder)
    {
        builder.Services.AddNatsClient(
            nats => nats.ConfigureOptions(opts => opts with
            {
                Url = builder.Configuration[nameof(MomusSettings.NatsUrl)] ?? opts.Url,
                AuthOpts = opts.AuthOpts with
                {
                    CredsFile = builder.Configuration[nameof(MomusSettings.CredsFilePath)],
                    Jwt = builder.Configuration[nameof(MomusSettings.Jwt)],
                    NKey = builder.Configuration[nameof(MomusSettings.NKey)],
                    Seed = builder.Configuration[nameof(MomusSettings.Seed)],
                    Token = builder.Configuration[nameof(MomusSettings.Token)],
                }
            })
        );
        
        builder.Services.AddHostedService<RouteConfigBackgroundService>();
    }
    private static void AddConfig(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddEnvironmentVariables("MS_");
        builder.Configuration.AddJsonFile("momusSettings.json", optional: true);
        builder.Services.Configure<MomusSettings>(builder.Configuration);
        builder.Services.AddTransient(x => x.GetRequiredService<IOptions<MomusSettings>>().Value);
    }
    private static void AddLogging(this WebApplicationBuilder builder)
    {
        // pre-app-startup logger
        Log.Logger = new LoggerConfiguration()
            .ConfigureLogger()
            .CreateBootstrapLogger();

        // post-app-startup logger
        builder.Host.UseSerilog(
            (_, services, configuration) => configuration
                .ReadFrom.Services(services)
                .ConfigureLogger()
        );

    }
    private static LoggerConfiguration ConfigureLogger(this LoggerConfiguration loggerConfiguration)
    {
        return loggerConfiguration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                LogEventLevel.Debug,
                "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}");
    }
}