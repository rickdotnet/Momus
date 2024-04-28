using Microsoft.Extensions.Options;
using Momus.Config;
using Momus.Middleware;
using NATS.Client.Core;
using NATS.Extensions.Microsoft.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace Momus;

public static class Setup
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder
            .AddLogging()
            .AddConfig();

        builder.Services.AddNatsClient(
            nats => nats.ConfigureOptions(opts => opts with
            {
                Url = builder.Configuration[nameof(MomusSettings.NatsUrl)] ?? opts.Url,
                AuthOpts = opts.AuthOpts with
                {
                    CredsFile = builder.Configuration[nameof(MomusSettings.CredsFilePath)],
                    Jwt = builder.Configuration[nameof(MomusSettings.Jwt)],
                    NKey = builder.Configuration[nameof(MomusSettings.NKey)],
                    Token = builder.Configuration[nameof(MomusSettings.Token)],
                }
            })
        );
        
        builder.Services.AddHostedService<RouteConfigBackgroundService>();

        // default routes before NATS KV read is complete
        builder.Services.AddReverseProxy()
            .LoadFromMemory(
                YarpDefaults.GetRoutes(),
                YarpDefaults.GetClusters()
            );

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        //app.UseForwardedHeaders();
        // for some reason UseForwardedHeaders() doesn't work properly, and I'm tired of trying to massage it
        //app.UseSchemeForwarding();
        app.UseWwwRedirect(); // must be added before MapReverseProxy
        app.UseRouting();
        app.MapReverseProxy(x => x.Use(YarpDefaults.ProxyLog));

        return app;
    }

    private static WebApplicationBuilder AddLogging(this WebApplicationBuilder builder)
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

        return builder;
    }

    private static WebApplicationBuilder AddConfig(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddEnvironmentVariables("MS_");
        builder.Configuration.AddJsonFile("momusSettings.json", optional: true);
        builder.Services.Configure<MomusSettings>(builder.Configuration);
        builder.Services.AddTransient(x => x.GetRequiredService<IOptions<MomusSettings>>().Value);

        return builder;
    }

    private static void UseWwwRedirect(this IApplicationBuilder builder)
        => builder.UseMiddleware<WwwRedirectMiddleware>();

    private static void UseSchemeForwarding(this IApplicationBuilder builder)
        => builder.UseMiddleware<SchemeForwardingMiddleware>();

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