using Momus.Cli.Services;
using Spectre.Console;

namespace Momus.Cli.Interactive;

public class InteractiveApp
{
    private readonly IRouteConfigService configService;
    private RouteMenu? routeMenu;

    public InteractiveApp(IRouteConfigService configService)
    {
        this.configService = configService;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        AnsiConsole.Write(
            new FigletText("Momus CLI")
                .LeftJustified()
                .Color(Color.Blue));

        AnsiConsole.MarkupLine("[grey]Interactive Route Management Console[/]");
        AnsiConsole.WriteLine();

        // Initialize route menu and load config immediately
        routeMenu = new RouteMenu(configService);
        await AnsiConsole.Status()
            .StartAsync("Loading routes...", async ctx =>
            {
                var config = await configService.LoadConfigAsync(cancellationToken);
                configService.SetCachedConfig(config);
            });

        while (!cancellationToken.IsCancellationRequested)
        {
            // Get cached config to show recent routes
            var cachedConfig = configService.GetCachedConfig();
            
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[blue]Recently Added Routes:[/]");
            
            if (cachedConfig?.Routes.Length > 0)
            {
                routeMenu!.DisplayRoutesTable(cachedConfig.Routes.Take(5).ToList(), cachedConfig);
                
                if (cachedConfig.Routes.Length > 5)
                {
                    AnsiConsole.MarkupLine($"[grey]... and {cachedConfig.Routes.Length - 5} more routes[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[grey]No routes configured[/]");
            }

            AnsiConsole.WriteLine();
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]What would you like to do?[/]")
                    .PageSize(10)
                    .AddChoices(new[]
                    {
                        "Add Route",
                        "Search Routes",
                        "Import/Export Config",
                        "Exit"
                    }));

            switch (choice)
            {
                case "Add Route":
                    await routeMenu!.AddRouteAsync(cancellationToken);
                    break;
                case "Search Routes":
                    await routeMenu!.SearchAndManageRoutesAsync(cancellationToken);
                    break;
                case "Import/Export Config":
                    await ImportExportConfigAsync(cancellationToken);
                    break;
                case "Exit":
                    AnsiConsole.MarkupLine("[yellow]Goodbye![/]");
                    return;
            }

            AnsiConsole.WriteLine();
        }
    }

    private async Task ImportExportConfigAsync(CancellationToken cancellationToken)
    {
        var configMenu = new ConfigMenu(configService);
        await configMenu.ShowAsync(cancellationToken);
    }
}
