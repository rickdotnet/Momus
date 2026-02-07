## Requirements

### Requirement: Load configuration from local config file
The system SHALL load configuration from `momusConfig.json` in the current working directory.

#### Scenario: Local config file exists
- **WHEN** the CLI starts and a `momusConfig.json` file exists in the current directory
- **THEN** the system SHALL load configuration values from that file

#### Scenario: Local config file does not exist
- **WHEN** the CLI starts and no `momusConfig.json` exists in the current directory
- **THEN** the system SHALL continue without error

### Requirement: Load configuration from home directory
The system SHALL load configuration from `~/.momus/momusConfig.json`.

#### Scenario: Home directory config exists
- **WHEN** the CLI starts and `~/.momus/momusConfig.json` exists
- **THEN** the system SHALL load configuration values from that file

#### Scenario: Home directory config does not exist
- **WHEN** the CLI starts and `~/.momus/momusConfig.json` does not exist
- **THEN** the system SHALL continue without error

### Requirement: Load configuration from environment variables
The system SHALL load configuration from environment variables with the `MOMUS_` prefix.

#### Scenario: Environment variables set
- **WHEN** environment variables with the `MOMUS_` prefix are set
- **THEN** the system SHALL load those values as configuration

### Requirement: Configuration priority resolution
The system SHALL apply configuration values with the priority: local config > home config > environment variables.

#### Scenario: Value exists in local config only
- **WHEN** a configuration value exists only in the local `momusConfig.json`
- **THEN** that value SHALL be used

#### Scenario: Value exists in home config only
- **WHEN** a configuration value exists only in `~/.momus/momusConfig.json`
- **THEN** that value SHALL be used

#### Scenario: Value exists in environment variables only
- **WHEN** a configuration value exists only in environment variables
- **THEN** that value SHALL be used

#### Scenario: Same value in multiple sources
- **WHEN** the same configuration key exists in multiple sources
- **THEN** the value from the highest priority source SHALL be used (local > home > env)

### Requirement: Support NATS connection configuration
The system SHALL support configuring NATS connection parameters: `NatsUrl`, `NatsUser`, `NatsPass`, and `NatsToken`.

#### Scenario: All NATS settings configured
- **WHEN** all NATS configuration values are provided through supported sources
- **THEN** the CLI SHALL use those values to establish the NATS connection

#### Scenario: Partial NATS configuration
- **WHEN** only some NATS configuration values are provided
- **THEN** the CLI SHALL use provided values and rely on defaults for missing values

#### Scenario: No NATS configuration
- **WHEN** no NATS configuration values are provided
- **THEN** the CLI SHALL use default NATS connection settings

### Requirement: Configuration file format
The system SHALL support JSON configuration files with a flat structure where keys match configuration property names.

#### Scenario: Valid JSON config file
- **WHEN** a configuration file contains valid JSON with supported keys
- **THEN** the system SHALL parse and load those configuration values

#### Scenario: Invalid JSON config file
- **WHEN** a configuration file contains invalid JSON
- **THEN** the system SHALL log an error and fall back to lower priority sources
