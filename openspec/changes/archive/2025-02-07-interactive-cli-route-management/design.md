## Context

The Momus reverse proxy uses YARP (Yet Another Reverse Proxy) for request routing and NATS KeyValue store for dynamic configuration. Currently, route configuration requires manual JSON editing or direct NATS KV interaction. The existing Momus.Cli project has a basic Program.cs that demonstrates putting a hardcoded config to NATS KV.

Current architecture:
- **YarpConfig**: Record containing RouteConfig[] and ClusterConfig[] arrays
- **NATS KV**: Stores serialized YarpConfig under store "momus", key "route-config"
- **MomusSettings**: Configuration for NATS connection (URL, credentials, store/key names)
- **YARP RouteConfig**: Contains RouteId, Match (Hosts, Path), ClusterId, Metadata
- **YARP ClusterConfig**: Contains ClusterId, Destinations dictionary

## Goals / Non-Goals

**Goals:**
- Provide an intuitive interactive CLI for CRUD operations on YARP routes and clusters
- Support both interactive (Spectre.Console) and non-interactive (System.CommandLine) modes
- Allow bulk operations via JSON import/export
- Validate configurations before persisting to NATS KV
- Display current configurations in readable tabular format
- Support array inputs for hosts/paths via comma-separated or multi-prompt input

**Non-Goals:**
- Real-time monitoring or log tailing (out of scope)
- Configuration versioning or history (NATS KV handles this)
- Authentication/authorization within the CLI (assumes NATS credentials provide access)
- Modifying YARP runtime behavior beyond configuration updates

## Decisions

### 1. Command Structure
Use System.CommandLine as the primary command dispatcher with subcommands:
- `momus routes` - List, add, edit, delete routes
- `momus clusters` - List, add, edit, delete clusters  
- `momus config` - Import, export, validate
- `momus interactive` - Launch interactive TUI mode (default when no args)

**Rationale**: System.CommandLine provides robust parsing, help generation, and extensibility. Subcommands map naturally to the domain model (routes vs clusters).

**Alternative considered**: Single flat command structure - rejected because it becomes unwieldy with many operations.

### 2. Interactive Mode with Spectre.Console
Use Spectre.Console for rich interactive prompts, tables, and validation:
- `SelectionPrompt` for choosing operations
- `TextPrompt` with validation for input
- `Table` for displaying configurations
- `Status` for showing progress during NATS operations

**Rationale**: Spectre.Console is the de facto standard for .NET CLI UX, providing polished prompts and excellent documentation.

**Alternative considered**: Built-in Console - rejected due to poor UX and no validation support.

### 3. NATS KV Integration Pattern
Create a `RouteConfigService` that encapsulates all NATS KV operations:
- Load full config from KV
- Modify in-memory
- Persist back to KV atomically
- Handle serialization/deserialization

**Rationale**: Encapsulation allows for easier testing and potential future caching or batching.

**Alternative considered**: Direct KV calls in command handlers - rejected due to code duplication and harder testing.

### 4. Validation Strategy
Validate at multiple layers:
1. **Input validation**: Spectre.Console validators for immediate feedback
2. **Business validation**: Ensure ClusterId references exist before saving routes
3. **Schema validation**: Verify serialized JSON is valid YARP config

**Rationale**: Early validation prevents errors and provides better UX.

### 5. Configuration Format for Import/Export
Use standard YARP JSON format matching YarpConfig structure:
```json
{
  "Routes": [...],
  "Clusters": [...]
}
```

**Rationale**: Maintains compatibility with existing YARP ecosystem and documentation.

## Risks / Trade-offs

**Risk**: NATS KV concurrent modification conflicts
- **Mitigation**: Use optimistic concurrency with version checking. On conflict, show error and reload current state.

**Risk**: Large configuration files causing performance issues
- **Mitigation**: Configurations are typically small (<100 routes). If scale increases, implement pagination in list views.

**Risk**: Breaking changes to YARP configuration schema
- **Mitigation**: Use YARP's built-in types. If schema changes, compilation will catch breaking changes.

**Trade-off**: Interactive mode requires more code than simple commands
- **Acceptance**: Better UX justifies additional complexity for the primary use case.

## Migration Plan

1. **Phase 1**: Implement core RouteConfigService and list operations
2. **Phase 2**: Add CRUD operations for routes
3. **Phase 3**: Add CRUD operations for clusters
4. **Phase 4**: Add import/export functionality
5. **Phase 5**: Polish interactive mode with Spectre.Console enhancements

Rollback: Remove CLI binaries. NATS KV data remains unaffected.

## Open Questions

- Should we support bulk operations (delete multiple routes at once)?
- Should we add a "dry-run" mode for imports to preview changes?
- Should we cache NATS connection or reconnect per operation?
