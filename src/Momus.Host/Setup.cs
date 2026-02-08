using Microsoft.Extensions.Options;
using Momus.Config;
using Momus.Middleware;
using NATS.Client.Core;
using NATS.Extensions.Microsoft.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace Momus.Host;

public static class Setup
{
    internal static void ConfigureBuilder(this WebApplicationBuilder builder)
    {
        builder.AddLogging();
        builder.AddConfig();
        builder.ConfigureNats();
        builder.Services.AddHostedService<RouteConfigBackgroundService>();
    }

    internal static void ConfigureApplication(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseMiddleware<WwwRedirectMiddleware>();
        // need to test this again with and without cloudflare in the middle
        //app.UseMiddleware<SchemeForwardingMiddleware>();
    }

    extension(WebApplicationBuilder builder)
    {
        private void ConfigureNats()
        {
            builder.Services.AddNatsClient(nats => nats
                .ConfigureOptions(optsBuilder => optsBuilder
                    .Configure(opts => opts.Opts = opts.Opts with
                        {
                            Url = builder.Configuration[nameof(MomusSettings.NatsUrl)] ?? opts.Opts.Url,
                            InboxPrefix = builder.Configuration[nameof(MomusSettings.User)] ?? Guid.NewGuid().ToString()[..8],
                            AuthOpts = new NatsAuthOpts
                            {
                                Username = builder.Configuration[nameof(MomusSettings.User)],
                                Password = builder.Configuration[nameof(MomusSettings.Pass)],
                                CredsFile = builder.Configuration[nameof(MomusSettings.CredsFilePath)],
                                Jwt = builder.Configuration[nameof(MomusSettings.Jwt)],
                                NKey = builder.Configuration[nameof(MomusSettings.NKey)],
                                Seed = builder.Configuration[nameof(MomusSettings.Seed)],
                                Token = builder.Configuration[nameof(MomusSettings.Token)],
                            }
                        }
                    )
                )
            );

            builder.Services.AddHostedService<RouteConfigBackgroundService>();
        }

        private void AddConfig()
        {
            builder.Configuration.AddEnvironmentVariables("MOMUS_");
            builder.Configuration.AddJsonFile("momusSettings.json", optional: true);
            builder.Services.Configure<MomusSettings>(builder.Configuration);
            builder.Services.AddTransient(x => x.GetRequiredService<IOptions<MomusSettings>>().Value);
        }

        private void AddLogging()
        {
            builder.Services.AddSerilog();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    LogEventLevel.Debug,
                    "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
                .CreateLogger();
        }
    }
}
