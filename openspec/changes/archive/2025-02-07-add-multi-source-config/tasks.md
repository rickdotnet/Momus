## 1. Update MomusSettings Record

- [x] 1.1 Add `NatsUser` property to `MomusSettings` record
- [x] 1.2 Add `NatsPass` property to `MomusSettings` record

## 2. Implement Multi-Source Configuration Loading in CLI

- [x] 2.1 Update `Program.cs` to get user profile directory using `Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)`
- [x] 2.2 Add local config file source: `.AddJsonFile("momusConfig.json", optional: true)` as first/highest priority
- [x] 2.3 Add home directory config file source: `.AddJsonFile(Path.Combine(homeDir, ".momus", "momusConfig.json"), optional: true)` as second priority
- [x] 2.4 Keep environment variables as lowest priority: `.AddEnvironmentVariables(prefix: "MOMUS_")`
- [x] 2.5 Wrap configuration building in try-catch to handle invalid JSON gracefully

## 3. Replace Hardcoded NATS Connection

- [x] 3.1 Remove hardcoded `Url = "nats://bigpi.local:4222"` from `NatsOpts`
- [x] 3.2 Uncomment and use `settings.NatsUrl` for NATS URL
- [x] 3.3 Add NATS username/password authentication using `settings.NatsUser` and `settings.NatsPass` when provided
- [x] 3.4 Add NATS token authentication using `settings.Token` when provided

## 4. Testing and Validation

- [x] 4.1 Test with local `momusConfig.json` file
- [x] 4.2 Test with `~/.momus/momusConfig.json` file
- [x] 4.3 Test with environment variables (`MOMUS_*`)
- [x] 4.4 Test priority resolution: verify local overrides home, home overrides env
- [x] 4.5 Test graceful handling when no config files exist
- [x] 4.6 Test invalid JSON error handling
- [x] 4.7 Verify NATS connection works with configured values

## 5. Documentation

- [x] 5.1 Add configuration section to CLI README or documentation
- [x] 5.2 Document config file locations and priority order
- [x] 5.3 Document supported configuration keys (NatsUrl, NatsUser, NatsPass, Token)
- [x] 5.4 Provide example `momusConfig.json` configuration
