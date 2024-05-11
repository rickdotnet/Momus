using Yarp.ReverseProxy.Configuration;

namespace Momus.Middleware;

public class WwwRedirectMiddleware
{
    private readonly RequestDelegate next;
    private readonly IProxyConfigProvider proxyConfigProvider;
    private readonly ILogger<WwwRedirectMiddleware> logger;

    public WwwRedirectMiddleware(RequestDelegate next, IProxyConfigProvider proxyConfigProvider, ILogger<WwwRedirectMiddleware> logger)
    {
        this.next = next;
        this.proxyConfigProvider = proxyConfigProvider;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var host = context.Request.Host;
        logger.LogWarning("WwwRedirectMiddleware - Host: {Host}", host.Host);

        // if the host starts with www, check if there's a route that matches it
        if (host.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            var nonWwwHost = host.Host.Substring(4);
            var config = proxyConfigProvider.GetConfig();
            var matchedRoute = config.Routes.FirstOrDefault(
                r => r.Match.Hosts != null &&
                     r.Match.Hosts.Any(x => x.Equals(nonWwwHost, StringComparison.OrdinalIgnoreCase)));

            if (matchedRoute == null)
                logger.LogError("WwwRedirectMiddleware - Matched route is null");
            
            // if the route has the RedirectWww metadata set to true, redirect to the non-www version
            if (matchedRoute?.Metadata != null
                && matchedRoute.Metadata.TryGetValue("RedirectWww", out var redirectWwwValue)
                && bool.TryParse(redirectWwwValue, out var redirectWww)
                && redirectWww)
            {
                var nonWwwUrl = new UriBuilder
                {
                    Scheme = context.Request.Scheme,
                    Host = nonWwwHost,
                    Path = context.Request.Path,
                    Query = context.Request.QueryString.ToString()
                };

                // only set the port if it's not the default port
                if (host.Port.HasValue && host.Port != 80)
                    nonWwwUrl.Port = host.Port.Value;

                logger.LogWarning("WwwRedirectMiddleware - Redirecting to {NonWwwUrl}", nonWwwUrl.ToString());
                context.Response.Redirect(nonWwwUrl.ToString(), permanent: true);
                return;
            }

            logger.LogError("WwwRedirectMiddleware - No route matched the host {Host}", nonWwwHost);
        }

        await next(context);
    }
}