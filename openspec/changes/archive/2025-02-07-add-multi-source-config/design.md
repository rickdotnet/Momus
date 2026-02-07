## Context

The Momus CLI currently has hardcoded NATS connection settings in `Program.cs`:
```csharp
var natsOpts = new NatsOpts
{
    Url = "nats://bigpi.local:4222"
    //Url = settings.NatsUrl,
};
```

The `MomusSettings` record exists with basic NATS configuration (`NatsUrl`, `CredsFilePath`, `Jwt`, `NKey`, `Seed`, `Token`) but lacks username/password authentication fields. The CLI currently only loads environment variables with the `MOMUS_` prefix.

Users need flexibility to configure NATS connections for different environments without code changes.

## Goals / Non-Goals

**Goals:**
- Enable configuration from three sources: local `momusConfig.json`, `~/.momus/momusConfig.json`, and environment variables
- Implement priority: local config > home config > environment variables
- Support NATS URL, username, password, and token configuration
- Replace hardcoded NATS connection with configured values
- Gracefully handle missing configuration files

**Non-Goals:**
- Support for YAML, TOML, or other config formats
- Configuration validation beyond JSON parsing
- Hot-reloading of configuration
- Encryption of configuration files
- Support for nested configuration objects (flat structure only)

## Decisions

### Use Microsoft.Extensions.Configuration builder pattern
**Rationale**: The project already uses this library for environment variables. It natively supports multiple configuration sources and handles priority through the builder chain order.

**Implementation**: Add JSON file sources in reverse priority order:
```csharp
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("momusConfig.json", optional: true)        // Highest priority
    .AddJsonFile(Path.Combine(homeDir, ".momus", "momusConfig.json"), optional: true)
    .AddEnvironmentVariables(prefix: "MOMUS_")              // Lowest priority
    .Build();
```

### Add NatsUser and NatsPass to MomusSettings
**Rationale**: The existing `MomusSettings` record already has `NatsUrl` but lacks username/password fields for NATS authentication. Adding these maintains consistency with the existing configuration model.

**Alternative Considered**: Use existing `CredsFilePath` for auth - rejected because username/password is a common and simpler auth pattern for many NATS deployments.

### Keep configuration flat (non-hierarchical)
**Rationale**: Simpler JSON files, easier environment variable mapping (`MOMUS_NatsUrl` vs `MOMUS_NATS__URL`). Consistent with current MomusSettings structure.

### Local config file name: `momusConfig.json`
**Rationale**: Matches the PascalCase convention used in the codebase. Located in working directory for project-specific overrides.

**Alternative Considered**: `.momusrc` or `.momus/config.json` - rejected in favor of explicit, discoverable naming.

### Home directory: `~/.momus/momusConfig.json`
**Rationale**: Standard Unix convention for user-level config. Creates a dedicated directory for future extensibility (credentials, history, etc.).

**Path Resolution**: Use `Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)` for cross-platform home directory detection.

## Risks / Trade-offs

**[Risk] Configuration file not found errors**
→ Mitigation: Use `optional: true` for all JSON file sources; configuration builder continues silently if files don't exist.

**[Risk] Invalid JSON causes runtime failure**
→ Mitigation: The `AddJsonFile` extension throws on invalid JSON. Wrap configuration building in try-catch to log helpful error message and continue with partial configuration.

**[Risk] Sensitive credentials in plaintext files**
→ Mitigation: Document this trade-off in README. Users can use environment variables for sensitive values. Consider future enhancement for encrypted config storage.

**[Risk] Breaking change to existing environment variable handling**
→ Mitigation: Environment variables remain supported with same `MOMUS_` prefix. Priority change only affects users who add config files (which didn't exist before).

**[Risk] Cross-platform home directory detection**
→ Mitigation: Use `Environment.SpecialFolder.UserProfile` which works on Windows, Linux, and macOS.

## Open Questions

- Should we add a `--config` CLI option to specify an alternative config file path?
- Should the CLI validate NATS connectivity on startup with a timeout?
- Should we support `${env:VAR}` syntax in JSON files for secrets injection?
