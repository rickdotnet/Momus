## ADDED Requirements

### Requirement: Interactive mode launch
The CLI SHALL provide an interactive mode that launches when no command-line arguments are provided or when the `interactive` command is invoked.

#### Scenario: Launch interactive mode with no arguments
- **WHEN** the user runs `momus` with no arguments
- **THEN** the interactive TUI mode SHALL launch showing a main menu

#### Scenario: Launch interactive mode explicitly
- **WHEN** the user runs `momus interactive`
- **THEN** the interactive TUI mode SHALL launch showing a main menu

### Requirement: Main menu navigation
The interactive mode SHALL present a main menu with options for managing routes, clusters, configuration, and exiting.

#### Scenario: Display main menu
- **WHEN** the interactive mode launches
- **THEN** a selection menu SHALL display with options: "Manage Routes", "Manage Clusters", "Import/Export Config", "Exit"

### Requirement: Route listing
The system SHALL display all configured routes in a tabular format showing RouteId, Hosts, Path, and ClusterId.

#### Scenario: List all routes
- **WHEN** the user selects "Manage Routes" from the main menu
- **THEN** a table SHALL display all routes with columns: RouteId, Hosts, Path, ClusterId
- **AND** an option to add, edit, delete, or return to main menu SHALL be presented

### Requirement: Route creation
The system SHALL allow users to create new routes through interactive prompts for RouteId, Hosts, Path, ClusterId, and Metadata.

#### Scenario: Create new route interactively
- **WHEN** the user selects "Add Route"
- **THEN** prompts SHALL appear for: RouteId (unique identifier), Hosts (comma-separated or multiple inputs), Path pattern, ClusterId (dropdown of existing clusters), Metadata key-value pairs
- **AND** the new route SHALL be persisted to NATS KV upon confirmation

#### Scenario: Validate route inputs
- **WHEN** the user enters invalid input (empty RouteId, invalid Path pattern, non-existent ClusterId)
- **THEN** validation errors SHALL display inline
- **AND** the user SHALL be prompted to correct the input

### Requirement: Route editing
The system SHALL allow users to edit existing routes by selecting from a list and modifying any field.

#### Scenario: Edit existing route
- **WHEN** the user selects "Edit Route"
- **THEN** a list of existing routes SHALL display for selection
- **AND** prompts SHALL appear pre-populated with current values for each field
- **AND** changes SHALL be persisted to NATS KV upon confirmation

### Requirement: Route deletion
The system SHALL allow users to delete routes with a confirmation prompt.

#### Scenario: Delete route with confirmation
- **WHEN** the user selects "Delete Route"
- **THEN** a list of existing routes SHALL display for selection
- **AND** a confirmation prompt SHALL appear showing the route to be deleted
- **AND** the route SHALL be removed from NATS KV only upon confirmation

### Requirement: Cluster management
The system SHALL provide similar CRUD operations for clusters including listing, creating, editing, and deleting clusters.

#### Scenario: Create cluster with destinations
- **WHEN** the user selects "Add Cluster"
- **THEN** prompts SHALL appear for: ClusterId (unique identifier), Destinations (address and optional health check path for each)
- **AND** the new cluster SHALL be persisted to NATS KV upon confirmation

#### Scenario: Prevent deletion of referenced clusters
- **WHEN** the user attempts to delete a cluster that is referenced by one or more routes
- **THEN** an error message SHALL display listing the dependent routes
- **AND** the deletion SHALL be blocked

### Requirement: Non-interactive commands
The CLI SHALL support non-interactive commands using System.CommandLine for automation and scripting.

#### Scenario: List routes non-interactively
- **WHEN** the user runs `momus routes list --format json`
- **THEN** a JSON array of all routes SHALL output to stdout

#### Scenario: Add route non-interactively
- **WHEN** the user runs `momus routes add --id <id> --hosts <hosts> --path <path> --cluster <clusterId>`
- **THEN** the route SHALL be created and persisted without interactive prompts
