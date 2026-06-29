# Bestiary
The Monster Manager maintains the collection of NPC stat blocks used by the encounter tracker.

## Storage
- A single master SQLite database holds all entries, at `{AppData}/DnDManager/bestiary.db` (the path is overridable via a campaign setting).
- `.bestiary` files are used only for import/export, not as the live store.
- Importing from a `.bestiary` file handles duplicates by name with a selectable mode: Skip, Overwrite, or Merge.

## Open5e import
Support importing from 3rd-party sources, primarily Open5e (open5e.com): search, select one or more monsters, fetch the full stat block, and import into the master DB.  Imported fields include:
- Core stats: name, size, type/subtype, alignment, AC (and description), HP / hit dice, speed, CR, senses, languages.
- Ability scores (STR–CHA) and a proficiency bonus derived from CR.
- Saving-throw and skill proficiencies.
- Attacks (attack bonus, damage dice & type, reach/range) and the multiattack description.
- Special abilities, bonus actions, reactions, and legendary actions (with the legendary action description / budget).
- The Open5e slug, retained so associated spells can be linked (see `Spells.md`).

## Manual editing
Every stat-block field can be created or edited manually in the Monster Manager, including ability scores, saves, skills, attacks, abilities, and legendary/bonus/reaction actions — used both for hand-built monsters and to override imported data.