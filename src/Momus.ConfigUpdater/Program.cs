// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;
using Yarp.ReverseProxy.Configuration;

var config = new RouteConfig
{
    RouteId = Guid.NewGuid().ToString(),
    Match = new RouteMatch
    {
        Hosts = ["rickdot.net"],
        Path = "{**catch-all}"
    },
    ClusterId = Guid.NewGuid().ToString(),
    Metadata = new Dictionary<string, string>(new KeyValuePair<string, string>[]
    {
        new("RedirectWww", "true"), // default?
        new("UseOriginalHostHeader", "true")
    })
    //.WithTransformRequestHeader("X-Forwarded-Proto", "https", false)
};

await using var nats = new NatsConnection();
var js = new NatsJSContext(nats);
var kv = new NatsKVContext(js);
var store = await kv.CreateStoreAsync("momus");

RouteConfig[] routeConfig = [config];
ClusterConfig[] clusterConfigs =
[
    new ClusterConfig
    {
        ClusterId = config.ClusterId,
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["rickdot"] = new()
            {
                Address = "http://cloud-app-ricknet:8080"
            }
        }
    }
];

await store.PutAsync("route-config", JsonSerializer.Serialize(new { Routes = routeConfig, Clusters = clusterConfigs }));