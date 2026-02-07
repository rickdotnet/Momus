using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Momus.Cli.Interactive;
using Momus.Cli.Services;
using Momus.Config;
using NATS.Client.Core;

var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
var homeConfigPath = Path.Combine(homeDir, ".momus", "momusConfig.json");

var currentDir = Directory.GetCurrentDirectory();
var currentDirConfigPath = Path.Combine(currentDir, "momusConfig.json");

var configurationBuilder = new ConfigurationBuilder()
    .AddJsonFile(homeConfigPath, optional: true)
    .AddJsonFile(currentDirConfigPath, optional: true)
    .AddEnvironmentVariables(prefix: "MOMUS_");

var configuration = configurationBuilder.Build();
var services = new ServiceCollection();

var settings = configuration.Get<MomusSettings>() ?? new MomusSettings();
services.AddSingleton(settings);

var natsOpts = new NatsOpts
{
    Url = settings.NatsUrl,
    AuthOpts = new NatsAuthOpts
    {
        Username = settings.User,
        Password = settings.Pass,
        Token = settings.Token
    }
};

var natsConnection = new NatsConnection(natsOpts);
await natsConnection.ConnectAsync();

services.AddSingleton(natsConnection);

services.AddSingleton<IRouteConfigService, RouteConfigService>();
services.AddSingleton<InteractiveApp>();

var serviceProvider = services.BuildServiceProvider();

// Launch interactive mode
var app = serviceProvider.GetRequiredService<InteractiveApp>();
await app.RunAsync();
