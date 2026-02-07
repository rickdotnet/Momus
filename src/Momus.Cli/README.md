# Momus.Cli - Configuration Management Tool

Momus.Cli is a modern command-line interface for managing application configuration using key-value stores.

## Installation

Build the project from source:
```bash
dotnet build -c Release
```

The compiled binary will be available at `bin/Release/net10.0/momus-cli`.

### Configuration

The CLI tool supports configuration from multiple sources with the following priority (highest to lowest):

1. **Local config file**: `momusConfig.json` in the current working directory
2. **User config file**: `~/.momus/momusConfig.json` in your home directory
3. **Environment variables**: Variables prefixed with `MOMUS_`

#### Configuration File Format

Create a `momusConfig.json` file with NATS connection settings:

```json
{
  "NatsUrl": "nats://localhost:4222",
  "NatsUser": "username",
  "NatsPass": "password",
  "Token": "auth-token"
}
```

#### Configuration Options

| Key | Description | Environment Variable |
|-----|-------------|---------------------|
| `NatsUrl` | NATS server URL | `MOMUS_NatsUrl` |
| `NatsUser` | Username for NATS authentication | `MOMUS_NatsUser` |
| `NatsPass` | Password for NATS authentication | `MOMUS_NatsPass` |
| `Token` | Authentication token for NATS | `MOMUS_Token` |
