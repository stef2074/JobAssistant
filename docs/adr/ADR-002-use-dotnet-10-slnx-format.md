# ADR-002: Use the .NET 10 `.slnx` Solution Format

## Status

Accepted

## Context

The JobAssistant project was created using the .NET 10 SDK. During solution creation, the `dotnet new sln` command generated a `.slnx` file instead of the traditional `.sln` file.

The `.slnx` format is the new XML-based solution format introduced by Microsoft and is the default format used by the .NET 10 CLI. Since JobAssistant is a new project with no legacy dependencies, there is no requirement to use the older `.sln` format.

## Decision

The project will use the `.slnx` solution format as the canonical solution file unless a future tooling requirement necessitates reverting to the traditional `.sln` format.

All solution management operations should be performed using the .NET CLI or IDEs that support the `.slnx` format.

## Consequences

### Positive

- Aligns with the current .NET 10 tooling and defaults.
- Uses the default solution format for new solutions created with the .NET 10 SDK.
- Avoids introducing the older `.sln` format where no compatibility requirement exists.
- Provides a modern, XML-based solution file that is easier to read and process.

### Negative

- Older versions of Visual Studio and third-party tools may not support `.slnx`.
- Team members must use tooling that supports the .NET 10 solution format.

## Alternatives Considered

### Continue using `.sln`

Rejected.

Although the traditional `.sln` format remains supported, there is no technical or business justification for using it instead of the default `.slnx` format for this project. Adopting the default `.slnx` format keeps the project aligned with the direction of the .NET platform.
