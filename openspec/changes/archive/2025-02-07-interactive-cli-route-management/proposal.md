## Why

The Momus reverse proxy currently lacks a user-friendly way to manage route configurations. Administrators must manually edit configuration files or interact directly with the NATS KeyValue store. An interactive CLI will provide an intuitive interface for CRUD operations on route mappings, reducing errors and improving operational efficiency.

## What Changes

- Add interactive CLI commands to Momus.Cli for route management
- Integrate Spectre.Console for rich prompts and table displays
- Add System.CommandLine for command structure and non-interactive support
- Implement CRUD operations: create, read, update, delete route entries
- Add import/export functionality for JSON configuration files
- Support array inputs for domain patterns and other list fields
- Add validation for route configurations before submission to NATS KV

## Capabilities

### New Capabilities
- `cli-route-management`: Interactive CLI for managing YARP route configurations via NATS KV
- `config-import-export`: JSON import/export functionality for route configurations

### Modified Capabilities
- None

## Impact

- **Momus.Cli project**: New interactive commands and services
- **Dependencies**: Adds Spectre.Console and System.CommandLine NuGet packages
- **NATS KV**: CLI will read from and write to the existing NATS KeyValue store
- **Configuration format**: Uses existing YARP route configuration schema
