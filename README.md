# DnD Manager

A cross-platform desktop application for managing Dungeons & Dragons campaigns, characters, and encounters — built with Avalonia UI.

## Features

- **Encounter Tracker** — Manage PCs and NPCs, roll initiative, track turns and rounds, view NPC details in an overlay, and adjust HP on the fly
- **Dice Roller** — Full D&D notation parsing with advantage/disadvantage, Halfling Luck, multi-roll support, and timestamped history
- **Bestiary** — Master monster database with Open5e import, `.bestiary` file support, multiattack, legendary actions, and reactions
- **Web Interface** — Read-only encounter view served over HTTPS with real-time SignalR updates and QR code access
- **Themes** — Five WCAG-compliant themes with UI scaling from 0.5x to 2.0x
- **Campaign Persistence** — Automatic SQLite save/load of all encounter state, dice history, notes, and settings

## Tech Stack

| Component | Technology |
|-----------|------------|
| Language | C# |
| Runtime | .NET 10.0 |
| UI Framework | Avalonia 11.3.12 |
| Architecture | MVVM (CommunityToolkit.Mvvm) |
| Database | SQLite (Microsoft.Data.Sqlite) |
| Web Server | Embedded Kestrel + SignalR |
| Markdown | Markdig |
| QR Codes | QRCoder |

## Installation

Download pre-built binaries for your platform from the [Releases](../../releases) page — no .NET SDK required.

| Platform | Binary |
|----------|--------|
| Linux x64 | `DnDManager` |
| Windows x64 | `DnDManager.exe` |
| macOS x64 | `DnDManager` |
| macOS ARM64 | `DnDManager` |

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Build & Run

```bash
# Build
dotnet build

# Run
dotnet run --project DnDManager

# Run tests
dotnet test
```

## Publishing

The included `publish.sh` script builds self-contained single-file binaries for all supported platforms:

```bash
./publish.sh
```

| Platform | RID |
|----------|-----|
| Linux x64 | `linux-x64` |
| Windows x64 | `win-x64` |
| macOS x64 | `osx-x64` |
| macOS ARM64 | `osx-arm64` |

Binaries are output to `publish/<rid>/`.

## Feature Details

<details>
<summary><strong>Encounter Tracker</strong></summary>

- Add PCs and NPCs with Passive Perception, Passive Insight, AC, and HP
- Roll or manually set initiative, with automatic sort
- Step through turns with active character highlighting and round counting
- NPC overlay panel showing full stat block, attacks with damage rolls, and effect text
- Legendary action budget tracking with per-turn reset (D&D RAW)
- Reaction tracking with per-creature-turn reset
- `[L]` and `[R]` badges in the character grid for legendary actions and reactions
- HP adjustment via delta input with +/- buttons
- Campaign notes with Markdown editing and live preview
- Save/load character collections as `.dnd` files (standalone SQLite databases)
- Auto-save on close, auto-load on startup

</details>

<details>
<summary><strong>Dice Roller</strong></summary>

- Standard D&D notation: `2d6+3`, `1d20`, `4d8-1`, etc.
- Any valid die size (d2 through d100)
- Advantage (`>`) and disadvantage (`<`) markers — d20 only
- Halfling Luck: `hd20` (reroll natural 1s once)
- Comma-separated multi-rolls: `1d20, 2d6+3, 1d8`
- Timestamped roll history with natural 1/20 highlighting
- Smart total display: d20/d100 show individual results, smaller dice show totals first

</details>

<details>
<summary><strong>Bestiary</strong></summary>

- Single master database stored at `{AppData}/DnDManager/bestiary.db`
- Import from `.bestiary` files with duplicate handling
- Import from Open5e API (special abilities, non-attack actions, legendary actions, reactions, bonus actions)
- Attack model: melee/ranged types, reach/range, multiple damage entries with damage types, effect text, computed average damage
- Multiattack stored as description text
- HP stored as dice notation (e.g. `10d10+10`) with dynamic average calculation
- Optional initiative modifier (falls back to DEX modifier)
- Bestiary dropdown in encounter tracker for quick NPC creation

</details>

<details>
<summary><strong>Web Interface</strong></summary>

- Read-only encounter view accessible from any device on the local network
- Embedded Kestrel HTTPS server with in-memory self-signed certificate
- Random unprivileged port assignment
- Real-time updates via SignalR push
- QR code for quick access (available in Settings and Encounter toolbar)
- NPCs displayed as generic "Monster N" names for DM secrecy
- PCs show name and initiative only
- Color-coded health bars: green (>75%), yellow (75–30%), red (<30%)
- Separate theme selector for the web interface

</details>

<details>
<summary><strong>Themes & UI Scaling</strong></summary>

All themes meet WCAG AA or higher contrast requirements.

| Theme | Style | Notes |
|-------|-------|-------|
| System | Auto | Follows OS light/dark preference |
| Parchment | Light | D&D-themed browns and tans |
| High-Contrast Light | Light | WCAG AAA, strong blue accents |
| High-Contrast Dark | Dark | WCAG AAA, default for dark systems |
| Purple | Dark | Violet/purple accent ramp |
| Arcane | Dark | Deep blues with turquoise accents |

- UI scaling: 0.5x to 2.0x in 0.25x increments
- All color values use `DynamicResource` for live theme switching

</details>

## Project Structure

```
DnD Manager/
├── DnDManager/
│   ├── Models/          # Data models (Character, Attack, BestiaryEntry, etc.)
│   ├── Views/           # Avalonia XAML views
│   ├── ViewModels/      # MVVM view models
│   ├── Services/        # Business logic (dice, encounter, persistence, web)
│   ├── Controls/        # Custom controls (MarkdownRenderer)
│   ├── Converters/      # Value converters
│   └── Web/             # Web interface (SignalR hub, static content)
├── docs/                # Feature specifications
├── publish.sh           # Multi-platform publish script
└── THIRD-PARTY-NOTICES  # Dependency licenses
```

## Licenses

This project's dependencies are licensed under MIT and BSD 2-Clause. See [THIRD-PARTY-NOTICES](THIRD-PARTY-NOTICES) for full details.
