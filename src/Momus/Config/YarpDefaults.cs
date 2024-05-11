using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace Momus.Config;

public static class YarpDefaults
{
    public static Task ProxyLog(HttpContext context, Func<Task> next, ILogger logger)
    {
        logger.LogDebug("Request Path: {Request}", context.Request.Path);

        var proxyFeature = context.GetReverseProxyFeature();
        logger.LogDebug("Route ({Route}), Cluster ({Cluster}), Path ({Path}) ",
            proxyFeature.Route.Config.RouteId,
            proxyFeature.Route.Config.ClusterId,
            proxyFeature.Route.Config.Match.Path);

        // TODO: debug handling; current thought: look for a debug header and route to a debug API

        // Important - required to move to the next step in the proxy pipeline
        return next();
    }

    /// <summary>
    /// This is temporary and is replaced in the RouteConfigService on app start
    /// </summary>
    public static RouteConfig[] GetRoutes()
    {
        return new[]
        {
            new RouteConfig()
            {
                RouteId = "cloud-default",
                ClusterId = "cloud-default",
                Match = new RouteMatch
                {
                    // Path or Hosts are required for each route. This catch-all pattern matches all request paths.
                    Path = "{**catch-all}"
                }
            }.WithTransformUseOriginalHostHeader(useOriginal: true)
        };
    }

    /// <summary>
    /// This is temporary and is replaced in the RouteConfigService on app start
    /// </summary>
    public static ClusterConfig[] GetClusters()
    {
        // TODO: add some debug meta data and ability to bypass destination
        //       and point to a debugging endpoint (basically, spit out debug values)

        // There are individual entries for dev/training/prod. They all point to the same app. I left them
        // separate here in case I need to quickly change them. But, this will either be DB driven or config
        // driven. Time will tell.
        return new[]
        {
            new ClusterConfig()
            {
                ClusterId = "cloud-default",
                Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                {
                    {
                        "cloud-default",
                        new DestinationConfig() { Address = "http://cloud-app-default-server" }
                    }
                },
            }
        };
    }
}