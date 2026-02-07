## ADDED Requirements

### Requirement: Configuration export
The system SHALL allow users to export the current route and cluster configuration to a JSON file.

#### Scenario: Export configuration interactively
- **WHEN** the user selects "Export Config" from the menu
- **THEN** a prompt SHALL ask for the output file path
- **AND** the complete configuration SHALL be written to the specified file in JSON format
- **AND** a success message SHALL display with the file path

#### Scenario: Export configuration non-interactively
- **WHEN** the user runs `momus config export --output <filepath>`
- **THEN** the complete configuration SHALL be written to the specified file without prompts
- **AND** the exit code SHALL be 0 on success

#### Scenario: Export to stdout
- **WHEN** the user runs `momus config export --output -`
- **THEN** the complete configuration SHALL be output to stdout in JSON format

### Requirement: Configuration import
The system SHALL allow users to import route and cluster configuration from a JSON file.

#### Scenario: Import configuration interactively
- **WHEN** the user selects "Import Config" from the menu
- **THEN** a prompt SHALL ask for the input file path
- **AND** the file SHALL be validated for correct JSON structure
- **AND** a preview of changes SHALL display
- **AND** upon confirmation, the configuration SHALL be persisted to NATS KV

#### Scenario: Import configuration non-interactively
- **WHEN** the user runs `momus config import --input <filepath>`
- **THEN** the configuration SHALL be imported without prompts
- **AND** the exit code SHALL be 0 on success

#### Scenario: Import with merge strategy
- **WHEN** the user runs `momus config import --input <filepath> --strategy merge`
- **THEN** imported routes and clusters SHALL be merged with existing configuration
- **AND** existing entries with matching IDs SHALL be updated
- **AND** new entries SHALL be added

#### Scenario: Import with replace strategy
- **WHEN** the user runs `momus config import --input <filepath> --strategy replace`
- **THEN** the existing configuration SHALL be completely replaced with the imported configuration

### Requirement: Import validation
The system SHALL validate imported configurations before persisting to NATS KV.

#### Scenario: Validate JSON structure
- **WHEN** the user attempts to import an invalid JSON file
- **THEN** an error message SHALL display indicating the JSON parsing error
- **AND** the import SHALL be aborted

#### Scenario: Validate YARP schema
- **WHEN** the user attempts to import a JSON file with invalid YARP configuration
- **THEN** an error message SHALL display listing validation errors
- **AND** the import SHALL be aborted

#### Scenario: Validate cluster references
- **WHEN** the user attempts to import routes referencing non-existent clusters
- **THEN** an error message SHALL display listing orphaned route references
- **AND** the import SHALL be aborted

### Requirement: Configuration format
The exported and imported JSON SHALL match the YarpConfig structure with Routes and Clusters arrays.

#### Scenario: Export format matches YarpConfig
- **WHEN** the configuration is exported
- **THEN** the JSON structure SHALL contain top-level "Routes" and "Clusters" arrays
- **AND** each route SHALL follow YARP RouteConfig schema
- **AND** each cluster SHALL follow YARP ClusterConfig schema
