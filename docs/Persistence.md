# Persistence
The running campaign is stored in a single SQLite database so it survives between sessions, consistent with the project's preference for binary / non-human-readable data formats (see `../.claude/CLAUDE.md`).

## Campaign database
- Location: `{AppData}/DnDManager/campaign.db`, created automatically on first run.
- Tables: `Characters`, `EncounterState` (active flag, round, active turn), `DiceHistory`, `CampaignNotes` (content + caret position), and `AppSettings` (key/value).

## Save / load behaviour
- On startup, the previous campaign is loaded automatically (characters, encounter state, dice history, notes, settings, window geometry).
- On close, the campaign state is saved automatically.
- Individual settings are persisted immediately when changed, including: selected theme, UI scale, web theme, web UI scale, web network address & port, window position/size/state, splitter ratios, and custom themes.

## Encounter files
- `.dnd` files are standalone encounter exports (their own SQLite databases) produced by the encounter tracker's save/load buttons — distinct from the live `campaign.db`. See `EncounterTracker.md`.