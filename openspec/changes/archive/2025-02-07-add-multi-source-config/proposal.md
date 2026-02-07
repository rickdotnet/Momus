## Why

The CLI currently hardcodes NATS connection settings and only supports environment variables for configuration. Users need a flexible way to configure NATS connection parameters (url, user, pass, token) through multiple sources with clear precedence, enabling easier setup in different environments without code changes.

## What Changes

- Add configuration file support for NATS settings in JSON format
- Support loading config from: working directory (`momusConfig.json`), home directory (`~/.momus/momusConfig.json`), and environment variables (`MOMUS_*`)
- Implement priority order: local config > home config > environment variables
- Add support for `NatsUrl`, `NatsUser`, `NatsPass`, and `NatsToken` configuration values
- Replace hardcoded NATS connection in CLI with configured values
- Update CLI initialization to load configuration from all sources

## Capabilities

### New Capabilities
- `cli-config-loading`: Multi-source configuration loading for CLI with priority-based resolution (local config file, home directory config, environment variables)

### Modified Capabilities
- (none - no existing spec requirements change, only implementation details)

## Impact

- **Momus.Cli**: Program.cs configuration builder, new configuration loading logic
- **Momus**: Potential new configuration models if needed beyond existing MomusSettings
- **Dependencies**: None added, uses built-in Microsoft.Extensions.Configuration
- **Breaking Changes**: None - existing behavior preserved when config files absent
