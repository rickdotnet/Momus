using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Momus.Config;

namespace Momus;

public static class Setup
{
    public static WebApplication BuildMomusWebApplication(this WebApplicationBuilder builder, 
        Action<WebApplicationBuilder>? configureBuilder,
        Action<WebApplication>? configureApp)
    {
        builder.Services.AddReverseProxy()
            .LoadFromMemory(
                YarpDefaults.GetRoutes(),
                YarpDefaults.GetClusters()
            );
        
        configureBuilder?.Invoke(builder);

        var app = builder.Build();
        configureApp?.Invoke(app);

        var logger
            = app.Services.GetService<ILogger>() ?? NullLogger.Instance;

        app.UseRouting();
        app.MapReverseProxy(x => x.Use((h, n) => YarpDefaults.ProxyLog(h, n, logger)));

        return app;
    }
    }