using System.Text.Json;
using Serilog;

namespace Momus.Middleware;

public class SchemeForwardingMiddleware
{
    private readonly RequestDelegate next;

    public SchemeForwardingMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        Log.Debug("Pre-Scheme: {Scheme}",context.Request.Scheme);
        if(context.Request.Scheme == "https")
            return next(context);
        
        var forwardedProto = context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault();
        Log.Debug("X-Forwarded-Proto: {XForwardedProto}",forwardedProto);
        if (!string.IsNullOrEmpty(forwardedProto))
        {
            context.Request.Scheme = forwardedProto;
        }
        else
        {
            var cfVisitor = context.Request.Headers["CF-Visitor"].FirstOrDefault();
            Log.Debug("CF-Visitor: {CFVisitor}",cfVisitor);
            if (!string.IsNullOrEmpty(cfVisitor))
            {
                try
                {
                    var cfVisitorJson = JsonSerializer.Deserialize<Dictionary<string, string>>(cfVisitor);
                    Log.Debug("CF-Visitor-Json: {CfVisitorJson}",cfVisitorJson);
                    if (cfVisitorJson != null && cfVisitorJson.TryGetValue("scheme", out var scheme))
                    {
                        Log.Debug("CF-Visitor-Json-scheme: {CfVisitorJsonScheme}",scheme);
                        context.Request.Scheme = scheme;
                    }
                }
                catch (Exception)
                {
                    // Ignore JSON parsing errors
                }
            }
        }
        
        Log.Debug("Post-Scheme: {Scheme}",context.Request.Scheme);
        return next(context);
    }
}