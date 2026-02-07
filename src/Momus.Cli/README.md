# Momus.Cli - Configuration Management Tool

Momus.Cli is a modern command-line interface for managing application configuration using key-value stores.

## Installation

Build the project from source:
```bash
dotnet build -c Release
```

The compiled binary will be available at `bin/Release/net10.0/momus-cli`.

## Usage

### Basic Commands

#### Get Configuration Value
```bash
momus-cli config get <key>
```

Example:
```bash
momus-cli config get ConnectionStrings__DefaultConnection
```

#### Set Configuration Value
```bash
momus-cli config set <key> <value>
```

Example:
```bash
momus-cli config set Logging__LogLevel__Default Information
```

#### List All Configuration
```bash
momus-cli config list
```

#### Migrate Configuration
```bash
momus-cli migrate
```
This command reads configuration from local files and environment variables and migrates them to the KV store.

### Configuration

The CLI tool can be configured using an `appsettings.json` file:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "Redis": {
    "ConnectionString": "localhost:6379",
    "Database": 0
  }
}
```

## KV Store Backends

### In-Memory Store (Default)
- Stores configuration in memory for the current session
- Useful for testing and temporary operations
- Data is lost when the process exits

### Redis Store
- Persistent configuration storage using Redis
- Configure via the `Redis` section in appsettings.json
- Requires Redis server running on the specified connection string

## Environment Variables

The CLI tool supports standard .NET environment variable configuration:

- `DOTNET_ENVIRONMENT`: Sets the environment (Development, Production, etc.)
- `Redis__ConnectionString`: Overrides Redis connection string
- `Logging__LogLevel__Default`: Sets default logging level

## Examples

### Setting up Redis Backend
1. Start Redis server
2. Create or modify `appsettings.json`:
   ```json
   {
     "Redis": {
       "ConnectionString": "localhost:6379"
     }
   }
   ```
3. Run CLI commands - they will automatically use Redis

### Migrating from appsettings.json
1. Place your `appsettings.json` file in the same directory as the CLI
2. Run:
   ```bash
   momus-cli migrate
   ```
3. All configuration values will be copied to the KV store

### Managing Application Configuration
```bash
# Set database connection
momus-cli config set ConnectionStrings__DefaultConnection "Server=localhost;Database=Momus;User Id=user;Password=password;"

# Enable debug logging
momus-cli config set Logging__LogLevel__Default Debug

# View all current configuration
momus-cli config list

# Get specific value
momus-cli config get ConnectionStrings__DefaultConnection
```

## Error Handling

- If a key is not found when using `get`, the CLI returns an error
- Invalid commands display help information
- Connection errors to Redis are logged and reported

## Integration with Applications

Applications using the Momus framework can automatically read from the same KV store that Momus.Cli writes to, providing a unified configuration management experience.