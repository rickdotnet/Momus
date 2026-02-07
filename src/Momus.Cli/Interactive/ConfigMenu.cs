using System.Text.Json;
using Momus.Cli.Services;
using Momus.Config;
using Spectre.Console;

namespace Momus.Cli.Interactive;

public class ConfigMenu
{
    private readonly IRouteConfigService configService;

    public ConfigMenu(IRouteConfigService configService)
    {
        this.configService = configService;
    }

    public async Task ShowAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            AnsiConsole.WriteLine();
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]Import/Export Configuration[/]")
                    .AddChoices(new[]
                    {
                        "Export Config",
                        "Import Config",
                        "Back to Main Menu"
                    }));

            switch (choice)
            {
                case "Export Config":
                    await ExportConfigAsync(cancellationToken);
                    break;
                case "Import Config":
                    await ImportConfigAsync(cancellationToken);
                    break;
                case "Back to Main Menu":
                    return;
            }
        }
    }

    private async Task ExportConfigAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[blue]Export Configuration[/]");

        var filePath = AnsiConsole.Prompt(
            new TextPrompt<string>("Output file path:")
                .DefaultValue("./momus-config.json")
                .Validate(fp => !string.IsNullOrWhiteSpace(fp)
                    ? ValidationResult.Success()
                    : ValidationResult.Error("File path cannot be empty")));

        try
        {
            // Use cached config if available, otherwise load from NATS
            var config = configService.GetCachedConfig() ?? await configService.LoadConfigAsync(cancellationToken);

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            await File.WriteAllTextAsync(filePath, json, cancellationToken);

            AnsiConsole.MarkupLine($"[green]Configuration exported to: {Markup.Escape(filePath)}[/]");
            AnsiConsole.MarkupLine($"[grey]Routes: {config.Routes.Length}, Clusters: {config.Clusters.Length}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to export configuration: {Markup.Escape(ex.Message)}[/]");
        }
    }

    private async Task ImportConfigAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[blue]Import Configuration[/]");

        var filePath = AnsiConsole.Prompt(
            new TextPrompt<string>("Input file path:")
                .Validate(fp => !string.IsNullOrWhiteSpace(fp) && File.Exists(fp)
                    ? ValidationResult.Success()
                    : ValidationResult.Error("File not found")));

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);

            YarpConfig? importedConfig;
            try
            {
                importedConfig = JsonSerializer.Deserialize<YarpConfig>(json);
            }
            catch (JsonException ex)
            {
                AnsiConsole.MarkupLine($"[red]Invalid JSON format: {Markup.Escape(ex.Message)}[/]");
                return;
            }

            if (importedConfig == null)
            {
                AnsiConsole.MarkupLine("[red]Failed to deserialize configuration[/]");
                return;
            }

            // Use cached config if available, otherwise load from NATS
            var currentConfig = configService.GetCachedConfig() ?? await configService.LoadConfigAsync(cancellationToken);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]Preview of changes:[/]");

            var previewTable = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Type")
                .AddColumn("Current")
                .AddColumn("Imported");

            previewTable.AddRow(
                "Routes",
                currentConfig.Routes.Length.ToString(),
                importedConfig.Routes.Length.ToString());

            previewTable.AddRow(
                "Clusters",
                currentConfig.Clusters.Length.ToString(),
                importedConfig.Clusters.Length.ToString());

            AnsiConsole.Write(previewTable);

            var strategy = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Import strategy:")
                    .AddChoices(new[]
                    {
                        "Merge (update existing, add new)",
                        "Replace (overwrite everything)"
                    }));

            YarpConfig finalConfig;

            if (strategy.StartsWith("Merge"))
            {
                finalConfig = MergeConfigs(currentConfig, importedConfig);
                AnsiConsole.MarkupLine("[grey]Using merge strategy[/]");
            }
            else
            {
                finalConfig = importedConfig;
                AnsiConsole.MarkupLine("[grey]Using replace strategy[/]");
            }

            if (!configService.ValidateConfig(finalConfig))
            {
                AnsiConsole.MarkupLine("[red]Configuration validation failed. Import aborted.[/]");
                return;
            }

            var confirm = AnsiConsole.Confirm("Proceed with import?", false);

            if (!confirm)
            {
                AnsiConsole.MarkupLine("[yellow]Import cancelled[/]");
                return;
            }

            await AnsiConsole.Status()
                .StartAsync("Importing configuration...", async _ =>
                {
                    await configService.SaveConfigAsync(finalConfig, cancellationToken);
                });

            // Update cached config
            configService.SetCachedConfig(finalConfig);
            AnsiConsole.MarkupLine("[green]Configuration imported successfully![/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to import configuration: {Markup.Escape(ex.Message)}[/]");
        }
    }

    private YarpConfig MergeConfigs(YarpConfig current, YarpConfig imported)
    {
        var mergedRoutes = current.Routes.ToList();
        foreach (var route in imported.Routes)
        {
            var existingIndex = mergedRoutes.FindIndex(r => r.RouteId == route.RouteId);
            if (existingIndex >= 0)
            {
                mergedRoutes[existingIndex] = route;
            }
            else
            {
                mergedRoutes.Add(route);
            }
        }

        var mergedClusters = current.Clusters.ToList();
        foreach (var cluster in imported.Clusters)
        {
            var existingIndex = mergedClusters.FindIndex(c => c.ClusterId == cluster.ClusterId);
            if (existingIndex >= 0)
            {
                mergedClusters[existingIndex] = cluster;
            }
            else
            {
                mergedClusters.Add(cluster);
            }
        }

        return new YarpConfig
        {
            Routes = mergedRoutes.ToArray(),
            Clusters = mergedClusters.ToArray()
        };
    }
}
