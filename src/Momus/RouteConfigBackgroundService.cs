using System.Text.Json;
using Momus.Config;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;
using Serilog;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace Momus;

public class RouteConfigBackgroundService : BackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly InMemoryConfigProvider proxyConfigProvider;

    public RouteConfigBackgroundService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        this.proxyConfigProvider = serviceProvider.GetRequiredService<InMemoryConfigProvider>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nats = serviceProvider.GetRequiredService<NatsConnection>();
        var momusSettings = serviceProvider.GetRequiredService<MomusSettings>();
        var js = new NatsJSContext(nats);
        var kv = new NatsKVContext(js);
        var store = await kv.CreateStoreAsync(momusSettings.StoreName, stoppingToken);

        var initialValue = await store.GetEntryAsync<string>(momusSettings.KeyName, cancellationToken: stoppingToken);
        UpdateRouteConfig(initialValue.Value);

        var watchOpts = new NatsKVWatchOpts
        {
            UpdatesOnly = true,
            IncludeHistory = false,
        };
        
        // not sure if we want the store to blow up if the value is serialized incorrectly 
        // opting to deserialize the value in the method below for now
        await foreach (var kvPair in store.WatchAsync<string>(opts: watchOpts ,cancellationToken: stoppingToken))
        {
            Log.Information("Key: {Key}, Value: {Value}", kvPair.Key, kvPair.Value);
            UpdateRouteConfig(kvPair.Value);
        }
        
    }

    private void UpdateRouteConfig(string? payload)
    {
        if (payload is null)
        {
            Log.Warning("Null payload? Someone is playing games with us");
            return;
        }

        // start cooking
        Log.Information("Cooking: {Payload}", payload);

        try
        {

            var config = JsonSerializer.Deserialize<YarpConfig>(payload);
            if (config is null)
            {
                Log.Warning("Failed to deserialize payload: {Payload}", payload);
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
            Log.Error(ex, "Failed to update route config");
        }
    }
}