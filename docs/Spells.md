# Spells
Spells are stored separately from the bestiary so they can be shared across monsters.

## Storage
- A dedicated SQLite database at `{AppData}/DnDManager/spells.db`.
- A `Spells` table (name, level, school, casting time, range, components, duration, concentration, ritual, description, higher-level text, classes, source) and a `MonsterSpells` junction table.
- Monsters and spells are linked by Open5e slug; the junction records the slot level, usage type (at-will / slot-based / innate-per-day), uses-per-day, and a pre-cast marker.

## Import & parsing
- Spells are auto-populated during Open5e monster import (see `Bestiary.md`): the monster's spellcasting text is parsed and the referenced spells are fetched and linked, with deduplication.
- Both regular **Spellcasting** and **Innate Spellcasting** blocks are parsed, extracting caster level, spell save DC, spell attack bonus, and the at-will / slot-based / innate-per-day spell lists.

## Display
- Spells appear in the NPC overlay (see `EncounterTracker.md`) grouped by level.
- Slot-based casting shows available / remaining slots with the ability to mark slots used, including upcasting leveled spells to higher slots.
- Damage/effect scaling is derived from the higher-level text; cantrip scaling follows caster level.