## 1. Project Setup

- [x] 1.1 Add Spectre.Console NuGet package to Momus.Cli
- [x] 1.2 Add System.CommandLine NuGet package to Momus.Cli
- [x] 1.3 Create Services folder structure in Momus.Cli
- [x] 1.4 Create Commands folder structure in Momus.Cli
- [x] 1.5 Create Interactive folder structure in Momus.Cli

## 2. Core NATS KV Service

- [x] 2.1 Create IRouteConfigService interface with load/save methods
- [x] 2.2 Implement RouteConfigService with NATS KV integration
- [x] 2.3 Add JSON serialization/deserialization for YarpConfig
- [x] 2.4 Add error handling for NATS connection failures
- [x] 2.5 Add configuration validation before save

## 3. Interactive Mode Framework

- [x] 3.1 Create InteractiveApp entry point for TUI mode
- [x] 3.2 Implement main menu with Spectre.Console SelectionPrompt
- [x] 3.3 Add "Manage Routes", "Manage Clusters", "Import/Export Config", "Exit" options
- [x] 3.4 Launch interactive mode when no arguments provided
- [x] 3.5 Add explicit `interactive` command support

## 4. Route Interactive Operations

- [x] 4.1 Implement route list display with Spectre.Console Table
- [x] 4.2 Create route selection prompt for edit/delete operations
- [x] 4.3 Implement Add Route with prompts for RouteId, Hosts, Path, ClusterId, Metadata
- [x] 4.4 Add input validation for route fields (non-empty, valid patterns)
- [x] 4.5 Implement Edit Route with pre-populated current values
- [x] 4.6 Implement Delete Route with confirmation prompt
- [x] 4.7 Add cluster dropdown selection when creating/editing routes

## 5. Cluster Interactive Operations

- [x] 5.1 Implement cluster list display with Spectre.Console Table
- [x] 5.2 Create cluster selection prompt for edit/delete operations
- [x] 5.3 Implement Add Cluster with prompts for ClusterId and Destinations
- [x] 5.4 Add multi-destination support with dynamic prompts
- [x] 5.5 Implement Edit Cluster with pre-populated current values
- [x] 5.6 Implement Delete Cluster with confirmation prompt
- [x] 5.7 Add validation to prevent deletion of referenced clusters

## 6. Import/Export Interactive Operations

- [x] 6.1 Implement Export Config with file path prompt
- [x] 6.2 Add JSON file writing with formatted output
- [x] 6.3 Implement Import Config with file path prompt
- [x] 6.4 Add JSON validation before import
- [x] 6.5 Add preview of changes before confirmation
- [x] 6.6 Implement merge vs replace strategy selection

## 7. Non-Interactive Commands - Routes

- [x] 7.1 Create `routes list` command with --format option (table/json)
- [x] 7.2 Create `routes add` command with --id, --hosts, --path, --cluster flags
- [x] 7.3 Create `routes edit` command with route selection and field updates
- [x] 7.4 Create `routes delete` command with --id flag and confirmation
- [x] 7.5 Add --force flag to skip confirmations in non-interactive mode

## 8. Non-Interactive Commands - Clusters

- [x] 8.1 Create `clusters list` command with --format option
- [x] 8.2 Create `clusters add` command with --id and --destinations flags
- [x] 8.3 Create `clusters edit` command for updating destinations
- [x] 8.4 Create `clusters delete` command with --id flag
- [x] 8.5 Add validation to prevent deletion of referenced clusters

## 9. Non-Interactive Commands - Config

- [x] 9.1 Create `config export` command with --output flag (file or stdout)
- [x] 9.2 Create `config import` command with --input and --strategy flags
- [x] 9.3 Implement merge strategy for import command
- [x] 9.4 Implement replace strategy for import command
- [x] 9.5 Add validation error reporting for import failures

## 10. Validation and Error Handling

- [x] 10.1 Add route input validators (RouteId uniqueness, valid Path patterns)
- [x] 10.2 Add cluster reference validation in routes
- [x] 10.3 Add JSON schema validation for imports
- [x] 10.4 Add NATS KV error handling with user-friendly messages
- [x] 10.5 Add file I/O error handling for import/export

## 11. Testing and Polish

- [x] 11.1 Test interactive mode end-to-end
- [x] 11.2 Test non-interactive commands with various inputs
- [x] 11.3 Test import/export with real YARP configurations
- [x] 11.4 Add progress indicators for NATS operations
- [x] 11.5 Add color coding and styling with Spectre.Console
- [x] 11.6 Review and update help text for all commands
