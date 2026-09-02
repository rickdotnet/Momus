using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Momus.Config;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;

namespace Momus.Cli.Services;

public class RouteConfigService : IRouteConfigService
{
    private readonly NatsConnection natsConnection;
    private readonly MomusSettings settings;
    private readonly ILogger<RouteConfigService> logger;
    private YarpConfig? cachedConfig;

    public RouteConfigService(NatsConnection natsConnection, MomusSettings settings)
    {
        this.natsConnection = natsConnection;
        this.settings = settings;
        this.logger = NullLogger<RouteConfigService>.Instance;
    }

    public YarpConfig? GetCachedConfig() => cachedConfig;

    public void SetCachedConfig(YarpConfig config) => cachedConfig = config;

    public async Task<YarpConfig> LoadConfigAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var js = new NatsJSContext(natsConnection);
            var kv = new NatsKVContext(js);
            var store = await kv.CreateStoreAsync(settings.StoreName, cancellationToken);

            try
            {
                var result = await store.GetEntryAsync<string>(settings.KeyName, cancellationToken: cancellationToken);
                if (string.IsNullOrEmpty(result.Value))
                {
                    return new YarpConfig();
                }

                var config = JsonSerializer.Deserialize<YarpConfig>(result.Value);
                return config ?? new YarpConfig();
            }
            catch (NatsKVKeyNotFoundException)
            {
                logger.LogWarning("Key not found in NATS KV, returning empty config");
                return new YarpConfig();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load config from NATS KV");
            throw new InvalidOperationException($"Failed to load configuration from NATS: {ex.Message}", ex);
        }
    }

    public async Task SaveConfigAsync(YarpConfig config, CancellationToken cancellationToken = default)
    {
        if (!ValidateConfig(config))
        {
            throw new InvalidOperationException("Configuration validation failed");
        }

        try
        {
            var js = new NatsJSContext(natsConnection);
            var kv = new NatsKVContext(js);
            var store = await kv.CreateStoreAsync(settings.StoreName, cancellationToken);

            var payload = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            await store.PutAsync(settings.KeyName, payload, cancellationToken: cancellationToken);
            logger.LogInformation("Configuration saved successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save config to NATS KV");
            throw new InvalidOperationException($"Failed to save configuration to NATS: {ex.Message}", ex);
        }
    }

    public bool ValidateConfig(YarpConfig config)
    {
        var clusterIds = new HashSet<string>(config.Clusters.Select(c => c.ClusterId));

        foreach (var route in config.Routes)
        {
            if (string.IsNullOrWhiteSpace(route.RouteId))
            {
                logger.LogError("Route with empty RouteId found");
                return false;
            }

            if (string.IsNullOrWhiteSpace(route.ClusterId))
            {
                logger.LogError("Route '{RouteId}' has empty ClusterId", route.RouteId);
                return false;
            }

            if (!clusterIds.Contains(route.ClusterId))
            {
                logger.LogError("Route '{RouteId}' references non-existent cluster '{ClusterId}'",
                    route.RouteId, route.ClusterId);

                return false;
            }
        }

        foreach (var cluster in config.Clusters)
        {
            if (string.IsNullOrWhiteSpace(cluster.ClusterId))
            {
                logger.LogError("Cluster with empty ClusterId found");
                return false;
            }

            if (cluster.Destinations == null || cluster.Destinations.Count == 0)
            {
                logger.LogError("Cluster '{ClusterId}' has no destinations", cluster.ClusterId);
                return false;
            }
        }

        return true;
    }
}
