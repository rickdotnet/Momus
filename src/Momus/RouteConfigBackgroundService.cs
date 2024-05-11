using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Momus.Config;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace Momus;

public class RouteConfigBackgroundService : BackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly InMemoryConfigProvider proxyConfigProvider;
    private readonly ILogger logger;
    public RouteConfigBackgroundService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        this.proxyConfigProvider = serviceProvider.GetRequiredService<InMemoryConfigProvider>();
        this.logger = serviceProvider.GetService<ILogger<RouteConfigBackgroundService>>() ?? NullLogger<RouteConfigBackgroundService>.Instance;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nats = serviceProvider.GetRequiredService<NatsConnection>();
        var momusSettings = serviceProvider.GetRequiredService<MomusSettings>();
        var js = new NatsJSContext(nats);
        var kv = new NatsKVContext(js);
        var store = await kv.CreateStoreAsync(momusSettings.StoreName, stoppingToken);


        // this creates an empty key if it doesn't exist
        // this keeps the watcher from blowing up
        var initialValue = await GetInitialValue();

        if (!string.IsNullOrEmpty(initialValue))
            UpdateRouteConfig(initialValue);

        var watchOpts = new NatsKVWatchOpts
        {
            UpdatesOnly = true,
            IncludeHistory = false,
        };
        
        // not sure if we want the store to blow up if the value is serialized incorrectly 
        // opting to deserialize the value in the method below for now
        await foreach (var kvPair in store.WatchAsync<string>(opts: watchOpts, cancellationToken: stoppingToken))
        {
            logger.LogInformation("Key: {Key}, Value: {Value}", kvPair.Key, kvPair.Value);
            UpdateRouteConfig(kvPair.Value);
        }

        return;

        async ValueTask<string> GetInitialValue()
        {
            try
            {
                var result = await store.GetEntryAsync<string>(momusSettings.KeyName, cancellationToken: stoppingToken);
                return result.Value ?? string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Failed to get initial value");
                logger.LogWarning("Creating empty key in store");
                await store.CreateAsync(momusSettings.KeyName, "", cancellationToken: stoppingToken);
                return string.Empty;
            }
        }
    }

    private void UpdateRouteConfig(string? payload)
    {
        if (payload is null)
        {
            logger.LogWarning("Null payload? Someone is playing games with us");
            return;
        }

        // start cooking
        logger.LogWarning("Cooking: {Payload}", payload);

        try
        {
            var config = JsonSerializer.Deserialize<YarpConfig>(payload);
            if (config is null)
            {
                logger.LogWarning("Failed to deserialize payload: {Payload}", payload);
                return;
            }

            foreach (var route in config.Routes)
            {
                var originalHostMeta = route.Metadata?.GetValueOrDefault("UseOriginalHostHeader", "false") ?? "false";
                if (bool.TryParse(originalHostMeta, out var useOriginalHostHeader) && useOriginalHostHeader)
                    route.WithTransformUseOriginalHostHeader();

                // we had this originally when these headers were not being set properly
                // uncomment this if it still isn't working
                // route.WithTransformRequestHeader("X-Forwarded-Proto", "https", false);
            }

            proxyConfigProvider.Update(config.Routes, config.Clusters);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update route config");
        }
    }
}