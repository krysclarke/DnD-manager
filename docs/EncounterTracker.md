# Encounter tracker
## Required Interface components:
- button to add a PC
	- require character and player names, the passive perception (PP), passive investigation (PI) and armour class (AC))
- button to add an NPC
	- can select a pre-existing NPC from a drop-down populated from the bestiary, or
	- enter details directly
		- require monster name, AC, Hit points (HP)
- button to save the list of characters
	- optional: filter to just PC's, just NPC's, or both (default).
- button to load a collection of characters from file.
	- Add any NPC's to the list already displayed
	- ask what to do with PC's if a clash of character and/or player name occurs.
- button to start/stop the encounter.
	- When an encounter is started
		- automatically roll initiatives for all the NPCs
		- ask the DM to enter the PC's initiative rolls
		- sort entries by initiative roll in descending order (PCs before NPCs in a tie)
		- 'start' the encounter.
	- When the encounter is stopped
		- clear all initiative rolls for all characters.
	- While an encounter is running / active, display the round number and highlight the active character.

## Character Display
Use a grid to display all characters
  - Use Avalonia's SharedSizeGroup feature
  - display Character name (also show player name for PC's), initiative, PP, AC, HP, conditions.
  - allow entering some notes for each character
  - For NPC's, include a text box with "+" and "-" buttons to quickly adjust the current HP: the user enters an unsigned delta value, then clicks "+" or "-" (clamped to 0..max HP).
  - Show an active-turn indicator (▶) beside the row whose turn it currently is.
  - Show `[L]` / `[R]` badges on NPC rows that have legendary actions / reactions respectively.

## NPC stat blocks & overlay
An NPC carries a stat block (persisted as a compact `StatsJson` snapshot, optionally linked to a bestiary entry): AC, current/max HP, ability scores, proficiency bonus, saving-throw & skill proficiencies, attacks (with damage), multiattack text, special abilities, bonus actions, legendary actions, reactions, and spells (see `Bestiary.md` and `Spells.md`).

The overlay shows the selected NPC's stat block and supports:
  - One-click rolling of attack & damage dice.
  - Saving throws: pick an ability (auto-fills the modifier from the stat block, with manual override) and roll normal / advantage / disadvantage.
  - Skill checks: pick a skill plus an ability override, then roll normal / advantage / disadvantage.
  - Legendary-action budget tracking (used / total, with undo), reset at the start of that NPC's turn.
  - A "reaction used" flag, reset at the start of that NPC's turn.
All such rolls are sent to the dice roller as labeled rolls (see `DiceRoller.md`).

## Dice Roller
The dice roller (see: `DiceRoller.md` for more details) should be displayed down the right-hand side, and visible at all times

Show the details of an NPC when selected from the tracker overview, allowing one-click rolling of attack & damage dice for use outside of an active combat encounter.  When in active combat, this should automatically appear when it is that NPC's turn.  This is an inline `SplitView` pane (right side) over the character listing only — not over the dice roller nor the campaign notes.  It opens when an NPC is selected or becomes the active turn, and hides when a PC is selected or it is explicitly closed.

## Initiative modifier
Each NPC may have an optional initiative modifier; when absent it falls back to the DEX modifier.  Multiattack is stored as a description text rather than as discrete attacks.

## Campaign notes
The bottom-most section will be to display/store campaign notes
- Plain text entry, supporting Markdown syntax.
- Display as parsed Markdown
- Remember caret position between edits

## Save/Load
The save/load buttons export and import a standalone encounter file: a SQLite database with the `.dnd` extension containing the characters, dice history, and campaign notes.  Loading supports selective import and the PC name-clash handling described above.

The running campaign itself (characters, current round and turn, dice rolling history, notes, active theme, and settings) is auto-saved on close and auto-loaded on startup — see `Persistence.md` for details.