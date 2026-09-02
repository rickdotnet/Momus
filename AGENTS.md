# AGENTS.md

Guidelines for AI agents working on the Momus codebase.

## Project Overview

Momus is a dynamic reverse proxy built with .NET 10, using YARP for request routing and NATS for real-time configuration updates.

**Projects:**
- `Momus` - Core library (class library)
- `Momus.Host` - ASP.NET Core web host
- `Momus.Cli` - CLI tool for configuration

## Build Commands

```bash
# Build all projects
dotnet build src/Momus.sln

# Build specific project
dotnet build src/Momus.Host/Momus.Host.csproj

# Build Release
dotnet build src/Momus.sln -c Release

# Run the host
dotnet run --project src/Momus.Host

# Run CLI
dotnet run --project src/Momus.Cli

# Publish (Release, Linux x64)
dotnet publish src/Momus.Host/Momus.Host.csproj -c Release -r linux-x64 -o ./app
```

## Testing

**No test projects currently exist.** When adding tests:
- Create test projects with naming: `Momus.Tests` or `Momus.Host.Tests`
- Use `dotnet test` to run tests
- Use `dotnet test --filter "FullyQualifiedName~TestMethodName"` to run single test

## Code Style

### Formatting
- Use file-scoped namespaces
- Use implicit usings (`<ImplicitUsings>enable</ImplicitUsings>`)
- Use nullable reference types (`<Nullable>enable</Nullable>`)
- Use 4-space indentation
- Max line length: 120 characters

### Naming Conventions
- **Classes/Records**: PascalCase (`RouteConfig`, `MomusSettings`)
- **Methods**: PascalCase (`GetRoutes()`, `ConfigureBuilder()`)
- **Properties**: PascalCase (`StoreName`, `KeyName`)
- **Private fields**: camelCase with `this.` prefix (`this.serviceProvider`, `this.logger`)
- **Constants**: PascalCase
- **Local variables**: camelCase with `var`

### Types
- Use `record` for configuration/data classes
- Use `init` setters for immutable configuration
- Use target-typed new expressions: `new RouteConfig()`
- Use collection expressions: `[]`, `[item1, item2]`

### Imports
- System namespaces first
- Microsoft namespaces second  
- Third-party namespaces third (NATS, Yarp, Serilog)
- Project namespaces last (Momus.*)
- Use `using static` sparingly

### Error Handling
- Use nullable checks: `if (value is null)` or `value?.Property`
- Use try-catch with specific logging
- Log warnings for expected failures, errors for unexpected
- Use `NullLogger.Instance` as fallback

### Patterns

**Setup/Configuration:**
```csharp
public static class Setup
{
    internal static void ConfigureBuilder(this WebApplicationBuilder builder) { }
    internal static void ConfigureApplication(this WebApplication app) { }
}
```

**Background Services:**
```csharp
public class MyService : BackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger logger;
    
    public MyService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        this.logger = serviceProvider.GetService<ILogger<MyService>>() ?? NullLogger<MyService>.Instance;
    }
}
```

**Middleware:**
```csharp
public class MyMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<MyMiddleware> logger;
    
    public MyMiddleware(RequestDelegate next, ILogger<MyMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);
    }
}
```

### Logging
- Use Serilog with structured logging
- Log levels: Debug for diagnostics, Warning for issues, Error for failures
- Include context: `logger.LogWarning("Key: {Key}, Value: {Value}", key, value)`

### Configuration
- Add settings via `builder.Configuration.AddJsonFile("settings.json", optional: true)`
- Add env vars with prefix: `builder.Configuration.AddEnvironmentVariables("MS_")`
- Use `IOptions<T>` pattern for DI

## Dependencies

**Key Packages:**
- `Yarp.ReverseProxy` - Reverse proxy functionality
- `NATS.Client.*` - NATS messaging
- `Serilog.AspNetCore` - Logging
- `System.CommandLine` - CLI parsing

## Architecture Notes

- Uses NATS KeyValue store for dynamic route configuration
- YARP handles HTTP request routing
- Configuration updates via background service watching NATS
- Middleware for www redirects and scheme forwarding
- No XML documentation comments required

## Docker

```bash
# Build Docker image
docker build -t momus .

# Run with compose (downloads from GitHub)
curl -sS -L https://raw.githubusercontent.com/rickdotnet/Momus/main/docker-compose.yml -o docker-compose.yml && docker compose up -d
```
