# DnD Manager

Desktop application for managing Dungeons & Dragons campaigns, characters, and sessions.

All the functionality of the main program needs to be, as much as possible, displayed within one window (load/save file dialog windows are expressly permitted).
The main interface needs to have separate tabs for:
- Encounter tracker (including dice roller)
- Monster Manager
- Other / Ancilliary

see `../docs/DiceRoller.md` for details about the dice roller.  This applies to all requirements and code related to dice rolls.

see `../docs/EncounterTracker.md` for details about the encounter tracker.  This applies to all requirements and code related to managing encounters.

see `../docs/Bestiary.md` for details about the bestiary.  This applies to all requirements and code relating to creating and managing NPC's.

see `../docs/InterfaceCustomisation.md` for details about themes and UI scaling.  This applies to all requirements and code relating to creating, selecting, and customising themes, as well as adjusting the size of the interface.

see `../docs/WebInterface.md` for details about the web interface.  This applies to all requirements and code related to the web interface.

## Code Style
See `rules/` for language-specific conventions.  These apply to all code written or modified in this project.

## Tech Stack

- **Language:** C#
- **Framework:** .NET 9.0, Avalonia UI
- **IDE:** JetBrains Rider

## Commands

```bash
# Build
dotnet build

# Run
dotnet run --project <ProjectName>

# Test
dotnet test

# Clean
dotnet clean
```

## Agent Behavior

- YOU MUST consider asking clarification questions before making assumptions about requirements or implementation details.

## Build & Deployment

- Target a single binary output (use `PublishSingleFile`, `SelfContained` where appropriate)

## Data Storage

- Prefer binary/non-human-readable formats for data files (e.g., SQLite, MessagePack, protobuf, or binary serialization) for space efficiency
- Avoid plain JSON/XML/YAML for persistent data storage

## Conventions

### Naming

- `PascalCase` for classes, methods, properties, and public members
- `_camelCase` for private fields (underscore prefix)
- `camelCase` for local variables and parameters
- `I` prefix for interfaces (e.g., `ICharacterService`)
- Async methods suffixed with `Async`

### Project Structure

- Keep models, views, and viewmodels in separate directories
- One class per file, filename matches class name
- Use namespaces matching folder structure

### General

- Prefer `var` when the type is obvious from the right-hand side
- Use nullable reference types (`<Nullable>enable</Nullable>`)
- Handle disposal with `using` statements or `IDisposable` pattern