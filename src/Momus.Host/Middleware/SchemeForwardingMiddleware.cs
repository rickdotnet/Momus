using System.Text.Json;

namespace Momus.Host.Middleware;

public class SchemeForwardingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<SchemeForwardingMiddleware> logger;

    public SchemeForwardingMiddleware(RequestDelegate next, ILogger<SchemeForwardingMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Request.Headers;
        var headersPretty = JsonSerializer.Serialize(headers, new JsonSerializerOptions { WriteIndented = true });
        logger.LogWarning("Headers: {Headers}", headersPretty);
        logger.LogDebug("Pre-Scheme: {Scheme}", context.Request.Scheme);
        if (context.Request.Scheme == "https")
            return next(context);

        var forwardedProto = context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault();
        logger.LogDebug("X-Forwarded-Proto: {XForwardedProto}", forwardedProto);
        if (!string.IsNullOrEmpty(forwardedProto))
        {
            context.Request.Scheme = forwardedProto;
        }
        else
        {
            var cfVisitor = context.Request.Headers["CF-Visitor"].FirstOrDefault();
            logger.LogDebug("CF-Visitor: {CFVisitor}", cfVisitor);
            if (!string.IsNullOrEmpty(cfVisitor))
            {
                try
                {
                    var cfVisitorJson = JsonSerializer.Deserialize<Dictionary<string, string>>(cfVisitor);
                    logger.LogDebug("CF-Visitor-Json: {CfVisitorJson}", cfVisitorJson);
                    if (cfVisitorJson != null && cfVisitorJson.TryGetValue("scheme", out var scheme))
                    {
                        logger.LogDebug("CF-Visitor-Json-scheme: {CfVisitorJsonScheme}", scheme);
                        context.Request.Scheme = scheme;
                    }
                }
                catch (Exception)
                {
                    // Ignore JSON parsing errors
                }
            }
        }

        var forwardedHost = context.Request.Headers["X-Forwarded-Host"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedHost))
        {
            logger.LogWarning("X-Forwarded-Host: {XForwardedHost}", forwardedHost);
            //context.Request.Host = HostString.FromUriComponent(forwardedHost);
        }

        logger.LogDebug("Post-Scheme: {Scheme}", context.Request.Scheme);
        return next(context);
    }
}