using Momus.Cli.Services;
using Momus.Config;
using Spectre.Console;
using Yarp.ReverseProxy.Configuration;

namespace Momus.Cli.Interactive;

public class RouteMenu
{
    private readonly IRouteConfigService configService;

    public RouteMenu(IRouteConfigService configService)
    {
        this.configService = configService;
    }

    public async Task ShowAsync(CancellationToken cancellationToken)
    {
        // Load config immediately on startup and cache it in the service
        YarpConfig config;
        await AnsiConsole.Status()
            .StartAsync("Loading routes...", async ctx =>
            {
                config = await configService.LoadConfigAsync(cancellationToken);
                configService.SetCachedConfig(config);
            });

        while (!cancellationToken.IsCancellationRequested)
        {
            // Get cached config from service
            var cachedConfig = configService.GetCachedConfig()!;

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[blue]Route Management[/]");
            
            // Show up to first 5 routes by default
            AnsiConsole.WriteLine();
            DisplayRoutesTable(cachedConfig.Routes.Take(5).ToList(), cachedConfig, "Recently Added Routes");
            
            if (cachedConfig.Routes.Length > 5)
            {
                AnsiConsole.MarkupLine($"[grey]... and {cachedConfig.Routes.Length - 5} more routes[/]");
            }

            AnsiConsole.WriteLine();
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]What would you like to do?[/]")
                    .AddChoices(new[]
                    {
                        "Add Route",
                        "Search Routes",
                        "Back to Main Menu"
                    }));

            switch (choice)
            {
                case "Add Route":
                    await AddRouteAsync(cancellationToken);
                    break;
                case "Search Routes":
                    await SearchAndManageRoutesAsync(cancellationToken);
                    break;
                case "Back to Main Menu":
                    return;
            }
        }
    }

    public async Task SearchAndManageRoutesAsync(CancellationToken cancellationToken)
    {
        var cachedConfig = configService.GetCachedConfig()!;
        
        AnsiConsole.WriteLine();
        
        // Search/filter prompt - empty shows all
        var searchTerm = AnsiConsole.Prompt(
            new TextPrompt<string>("[grey]Search routes (Enter to show all):[/]")
                .AllowEmpty());

        var filteredRoutes = FilterRoutes(cachedConfig.Routes, searchTerm);

        AnsiConsole.WriteLine();
        if (filteredRoutes.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No routes match your search[/]");
            AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
            Console.ReadKey(true);
            return;
        }

        // Show filtered results in a table first
        DisplayRoutesTable(filteredRoutes, cachedConfig, $"Search Results ({filteredRoutes.Count} routes)");
        AnsiConsole.WriteLine();

        // Let user select a route from the filtered list
        var routeDisplay = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a route:")
                .PageSize(10)
                .AddChoices(filteredRoutes.Select(r => $"{r.RouteId} ({FormatHosts(r.Match?.Hosts)})")));

        // Extract route ID from the display string
        var selectedRouteId = routeDisplay.Split(' ')[0];
        var selectedRoute = filteredRoutes.First(r => r.RouteId == selectedRouteId);

        // Show detailed route info
        DisplayRouteDetails(selectedRoute, cachedConfig);

        AnsiConsole.WriteLine();
        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green]What would you like to do?[/]")
                .AddChoices(new[] { "Edit", "Delete", "Go Back" }));

        switch (action)
        {
            case "Edit":
                await EditRouteAsync(selectedRoute, cancellationToken);
                break;
            case "Delete":
                await DeleteRouteAsync(selectedRoute, cancellationToken);
                break;
            case "Go Back":
                return;
        }
    }

    private void DisplayRouteDetails(RouteConfig route, YarpConfig config)
    {
        var cluster = config.Clusters.FirstOrDefault(c => c.ClusterId == route.ClusterId);
        var destinations = cluster?.Destinations?.Select(d => d.Value.Address).ToList() ?? new List<string>();
        var metadata = route.Metadata ?? new Dictionary<string, string>();

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[blue]Route Details:[/]");
        
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Property", c => c.Width(15))
            .AddColumn("Value");

        table.AddRow("Route ID", Markup.Escape(route.RouteId));
        table.AddRow("Hosts", Markup.Escape(FormatHosts(route.Match?.Hosts)));
        table.AddRow("Path", Markup.Escape(route.Match?.Path ?? "{**catch-all}"));
        table.AddRow("Destinations", Markup.Escape(string.Join(", ", destinations)));
        table.AddRow("Redirect www", metadata.GetValueOrDefault("RedirectWww") == "true" ? "Yes" : "No");
        
        // Show other metadata
        var otherMetadata = metadata.Where(m => m.Key != "RedirectWww").ToList();
        if (otherMetadata.Count > 0)
        {
            table.AddRow("Metadata", Markup.Escape(string.Join(", ", otherMetadata.Select(m => $"{m.Key}={m.Value}"))));
        }

        AnsiConsole.Write(table);
    }

    private List<RouteConfig> FilterRoutes(RouteConfig[] routes, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return routes.ToList();

        var term = searchTerm.ToLowerInvariant();
        return routes.Where(r =>
            r.RouteId.ToLowerInvariant().Contains(term) ||
            (r.Match?.Hosts?.Any(h => h.ToLowerInvariant().Contains(term)) ?? false) ||
            (r.Match?.Path?.ToLowerInvariant().Contains(term) ?? false) ||
            r.ClusterId.ToLowerInvariant().Contains(term)
        ).ToList();
    }

    public void DisplayRoutesTable(List<RouteConfig> routes, YarpConfig config, string? title = null)
    {
        if (!string.IsNullOrEmpty(title))
        {
            AnsiConsole.MarkupLine($"[blue]{title}:[/]");
        }
        
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Route ID", c => c.Width(12))
            .AddColumn("Hosts", c => c.Width(35))
            .AddColumn("Path", c => c.Width(20))
            .AddColumn("Destination", c => c.Width(30));

        foreach (var route in routes)
        {
            var routeId = route.RouteId.Length > 10 
                ? route.RouteId[..8] + ".." 
                : route.RouteId;
            
            var hosts = FormatHosts(route.Match?.Hosts);
            var path = route.Match?.Path ?? "{**catch-all}";
            
            var cluster = config.Clusters.FirstOrDefault(c => c.ClusterId == route.ClusterId);
            var destination = cluster?.Destinations?.FirstOrDefault().Value?.Address ?? "N/A";
            if (destination.Length > 28)
                destination = destination[..26] + "..";

            table.AddRow(
                Markup.Escape(routeId),
                Markup.Escape(hosts),
                Markup.Escape(path),
                Markup.Escape(destination));
        }

        AnsiConsole.Write(table);
    }

    private string FormatHosts(IReadOnlyList<string>? hosts)
    {
        if (hosts == null || hosts.Count == 0)
            return "";

        if (hosts.Count <= 3)
            return string.Join(", ", hosts);

        var shown = hosts.Take(2).ToList();
        var remaining = hosts.Count - 2;
        shown.Add($"(+{remaining} more)");
        return string.Join(", ", shown);
    }

    public async Task AddRouteAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]Add New Route[/]");

        var cachedConfig = configService.GetCachedConfig()!;

        // Hosts
        var hostsInput = AnsiConsole.Prompt(
            new TextPrompt<string>("[grey]Hosts (comma-separated, empty for catch-all):[/]")
                .AllowEmpty());

        var hosts = string.IsNullOrWhiteSpace(hostsInput)
            ? null
            : hostsInput.Split(',').Select(h => h.Trim()).Where(h => !string.IsNullOrEmpty(h)).ToList();

        // Path
        var path = AnsiConsole.Prompt(
            new TextPrompt<string>("Path pattern:")
                .DefaultValue("{**catch-all}")
                .Validate(p => !string.IsNullOrWhiteSpace(p)
                    ? ValidationResult.Success()
                    : ValidationResult.Error("Path cannot be empty")));

        // Destinations
        var destinationsInput = AnsiConsole.Prompt(
            new TextPrompt<string>("Destinations (comma-separated URLs):")
                .Validate(d => !string.IsNullOrWhiteSpace(d) && d.Split(',').All(url => Uri.TryCreate(url.Trim(), UriKind.Absolute, out _))
                    ? ValidationResult.Success()
                    : ValidationResult.Error("Please provide valid URLs")));

        var destinationUrls = destinationsInput.Split(',').Select(u => u.Trim()).ToList();

        // Redirect www (separate from metadata)
        var redirectWww = AnsiConsole.Confirm("Redirect www to non-www?", true);

        // Additional metadata (simplified - single prompt, blank to skip)
        var metadata = new Dictionary<string, string>();
        
        var metaInput = AnsiConsole.Prompt(
            new TextPrompt<string>("[grey]Metadata (KEY=VALUE, or blank to skip):[/]")
                .AllowEmpty());

        if (!string.IsNullOrWhiteSpace(metaInput) && metaInput.Contains('='))
        {
            var parts = metaInput.Split('=', 2);
            metadata[parts[0].Trim()] = parts[1].Trim();
        }

        // Generate IDs
        var routeId = Guid.NewGuid().ToString("N")[..8];
        var clusterId = routeId;

        // Create route
        var route = new RouteConfig
        {
            RouteId = routeId,
            Match = new RouteMatch
            {
                Hosts = hosts,
                Path = path
            },
            ClusterId = clusterId,
            Metadata = BuildMetadata(redirectWww, metadata)
        };

        // Create cluster
        var destDict = new Dictionary<string, DestinationConfig>();
        for (int i = 0; i < destinationUrls.Count; i++)
        {
            destDict[$"dest{i + 1}"] = new DestinationConfig { Address = destinationUrls[i] };
        }

        var cluster = new ClusterConfig
        {
            ClusterId = clusterId,
            Destinations = destDict
        };

        // Show summary
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Route Summary:[/]");
        AnsiConsole.MarkupLine($"  Route ID: {Markup.Escape(routeId)}");
        AnsiConsole.MarkupLine($"  Hosts: {Markup.Escape(FormatHosts(hosts))}");
        AnsiConsole.MarkupLine($"  Path: {Markup.Escape(path)}");
        AnsiConsole.MarkupLine($"  Destinations: {Markup.Escape(string.Join(", ", destinationUrls))}");
        AnsiConsole.MarkupLine($"  Redirect www: {(redirectWww ? "Yes" : "No")}");
        if (metadata.Count > 0)
            AnsiConsole.MarkupLine($"  Metadata: {Markup.Escape(string.Join(", ", metadata.Select(m => $"{m.Key}={m.Value}")))}");

        if (!AnsiConsole.Confirm("Create this route?"))
        {
            AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
            return;
        }

        // Save
        var newRoutes = cachedConfig.Routes.ToList();
        newRoutes.Add(route);

        var newClusters = cachedConfig.Clusters.ToList();
        newClusters.Add(cluster);

        var newConfig = new YarpConfig
        {
            Routes = newRoutes.ToArray(),
            Clusters = newClusters.ToArray()
        };

        await AnsiConsole.Status()
            .StartAsync("Saving route...", async ctx =>
            {
                await configService.SaveConfigAsync(newConfig, cancellationToken);
            });

        configService.SetCachedConfig(newConfig);
        AnsiConsole.MarkupLine("[green]Route created successfully![/]");
    }

    private async Task EditRouteAsync(RouteConfig route, CancellationToken cancellationToken)
    {
        var cachedConfig = configService.GetCachedConfig()!;
        var cluster = cachedConfig.Clusters.FirstOrDefault(c => c.ClusterId == route.ClusterId);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[green]Editing Route: {Markup.Escape(route.RouteId)}[/]");

        // Hosts
        var currentHosts = route.Match?.Hosts != null ? string.Join(", ", route.Match.Hosts) : "";
        var hostsInput = AnsiConsole.Prompt(
            new TextPrompt<string>("[grey]Hosts (comma-separated, empty for catch-all):[/]")
                .DefaultValue(currentHosts)
                .AllowEmpty());

        var hosts = string.IsNullOrWhiteSpace(hostsInput)
            ? null
            : hostsInput.Split(',').Select(h => h.Trim()).Where(h => !string.IsNullOrEmpty(h)).ToList();

        // Path
        var path = AnsiConsole.Prompt(
            new TextPrompt<string>("Path pattern:")
                .DefaultValue(route.Match?.Path ?? "{**catch-all}")
                .Validate(p => !string.IsNullOrWhiteSpace(p)
                    ? ValidationResult.Success()
                    : ValidationResult.Error("Path cannot be empty")));

        // Destinations
        var currentDestinations = cluster?.Destinations?.Select(d => d.Value.Address).ToList() ?? new List<string>();
        var currentDestString = string.Join(", ", currentDestinations);
        
        var destinationsInput = AnsiConsole.Prompt(
            new TextPrompt<string>("Destinations (comma-separated URLs):")
                .DefaultValue(currentDestString)
                .Validate(d => !string.IsNullOrWhiteSpace(d) && d.Split(',').All(url => Uri.TryCreate(url.Trim(), UriKind.Absolute, out _))
                    ? ValidationResult.Success()
                    : ValidationResult.Error("Please provide valid URLs")));

        var destinationUrls = destinationsInput.Split(',').Select(u => u.Trim()).ToList();

        // Parse current metadata to extract redirectWww
        var currentMetadata = route.Metadata ?? new Dictionary<string, string>();
        var currentRedirectWww = currentMetadata.GetValueOrDefault("RedirectWww") == "true";
        
        // Redirect www (separate question)
        var redirectWww = AnsiConsole.Confirm("Redirect www to non-www?", currentRedirectWww);

        // Edit existing metadata (non-RedirectWww)
        var metadata = new Dictionary<string, string>();
        var existingMetadata = currentMetadata.Where(m => m.Key != "RedirectWww").ToList();
        
        if (existingMetadata.Count > 0)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[blue]Review metadata:[/]");
            
            foreach (var meta in existingMetadata)
            {
                var keep = AnsiConsole.Confirm($"Keep {Markup.Escape(meta.Key)}={Markup.Escape(meta.Value)}?", true);
                
                if (keep)
                {
                    metadata[meta.Key] = meta.Value;
                }
                // If No, we don't add it back (effectively deleting it)
            }
        }

        // Add new metadata
        AnsiConsole.WriteLine();
        while (true)
        {
            var newMeta = AnsiConsole.Prompt(
                new TextPrompt<string>("[grey]Add new metadata (KEY=VALUE, or blank to skip):[/]")
                    .AllowEmpty());
            
            if (string.IsNullOrWhiteSpace(newMeta))
                break;
            
            if (newMeta.Contains('=') && !string.IsNullOrWhiteSpace(newMeta.Split('=')[0]))
            {
                var parts = newMeta.Split('=', 2);
                metadata[parts[0].Trim()] = parts[1].Trim();
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Invalid format. Use KEY=VALUE[/]");
            }
        }

        // Update route
        var updatedRoute = route with
        {
            Match = new RouteMatch
            {
                Hosts = hosts,
                Path = path
            },
            Metadata = BuildMetadata(redirectWww, metadata)
        };

        // Update cluster destinations
        var destDict = new Dictionary<string, DestinationConfig>();
        for (int i = 0; i < destinationUrls.Count; i++)
        {
            destDict[$"dest{i + 1}"] = new DestinationConfig { Address = destinationUrls[i] };
        }

        var updatedCluster = cluster with
        {
            Destinations = destDict
        };

        // Save
        var newRoutes = cachedConfig.Routes.Select(r => r.RouteId == route.RouteId ? updatedRoute : r).ToArray();
        var newClusters = cachedConfig.Clusters.Select(c => c.ClusterId == cluster?.ClusterId ? updatedCluster : c).ToArray();

        var newConfig = new YarpConfig
        {
            Routes = newRoutes,
            Clusters = newClusters
        };

        await AnsiConsole.Status()
            .StartAsync("Saving changes...", async ctx =>
            {
                await configService.SaveConfigAsync(newConfig, cancellationToken);
            });

        configService.SetCachedConfig(newConfig);
        AnsiConsole.MarkupLine("[green]Route updated successfully![/]");
        
        // Get the updated route from the cached config and redisplay
        var updatedCachedConfig = configService.GetCachedConfig()!;
        var refreshedRoute = updatedCachedConfig.Routes.First(r => r.RouteId == route.RouteId);
        
        // Redisplay the route details
        DisplayRouteDetails(refreshedRoute, updatedCachedConfig);
        
        AnsiConsole.WriteLine();
        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green]What would you like to do?[/]")
                .AddChoices(new[] { "Edit", "Delete", "Go Back" }));

        switch (action)
        {
            case "Edit":
                await EditRouteAsync(refreshedRoute, cancellationToken);
                break;
            case "Delete":
                await DeleteRouteAsync(refreshedRoute, cancellationToken);
                break;
            case "Go Back":
                return;
        }
    }

    private async Task DeleteRouteAsync(RouteConfig route, CancellationToken cancellationToken)
    {
        var cachedConfig = configService.GetCachedConfig()!;
        var cluster = cachedConfig.Clusters.FirstOrDefault(c => c.ClusterId == route.ClusterId);

        // Show summary
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[red]You are about to delete:[/]");
        AnsiConsole.MarkupLine($"  Route ID: {Markup.Escape(route.RouteId)}");
        AnsiConsole.MarkupLine($"  Hosts: {Markup.Escape(FormatHosts(route.Match?.Hosts))}");
        AnsiConsole.MarkupLine($"  Path: {Markup.Escape(route.Match?.Path ?? "{**catch-all}")}");
        
        if (cluster?.Destinations != null)
        {
            var dests = string.Join(", ", cluster.Destinations.Select(d => d.Value.Address));
            AnsiConsole.MarkupLine($"  Destinations: {Markup.Escape(dests)}");
        }

        AnsiConsole.WriteLine();
        if (!AnsiConsole.Confirm("Are you sure you want to delete this route?", false))
        {
            AnsiConsole.MarkupLine("[yellow]Deletion cancelled[/]");
            return;
        }

        // Delete
        var newRoutes = cachedConfig.Routes.Where(r => r.RouteId != route.RouteId).ToArray();
        var newClusters = cachedConfig.Clusters.Where(c => c.ClusterId != route.ClusterId).ToArray();

        var newConfig = new YarpConfig
        {
            Routes = newRoutes,
            Clusters = newClusters
        };

        await AnsiConsole.Status()
            .StartAsync("Deleting route...", async ctx =>
            {
                await configService.SaveConfigAsync(newConfig, cancellationToken);
            });

        configService.SetCachedConfig(newConfig);
        AnsiConsole.MarkupLine("[green]Route deleted successfully![/]");
    }

    private Dictionary<string, string>? BuildMetadata(bool redirectWww, Dictionary<string, string> additionalMetadata)
    {
        var metadata = new Dictionary<string, string>();
        
        if (redirectWww)
            metadata["RedirectWww"] = "true";
        
        foreach (var meta in additionalMetadata)
        {
            metadata[meta.Key] = meta.Value;
        }

        return metadata.Count > 0 ? metadata : null;
    }
}
