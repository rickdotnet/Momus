using Yarp.ReverseProxy.Configuration;

namespace Momus.Config;

public record YarpConfig
{
    public RouteConfig[] Routes { get; init; } = [];
    public ClusterConfig[] Clusters { get; init; } = [];
}