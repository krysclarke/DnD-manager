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
  - For NPC's, include a text box with "+" and "-" buttons to quickly increase/decrease the current HP.

## Dice Roller
The dice roller (see: `DiceRoller.md` for more details) should be displayed down the right-hand side, and visible at all times

Show the details of an NPC when selected from the tracker overview, allowing one-click rolling of attack & damage dice for use outside of an active combat encounter.  When in active combat, this should automatically appear when it is that NPC's turn.  This should be an overlay on top of the character listing, but not the dice roller nor the campaign notes.

## Campaign notes
The bottom-most section will be to display/store campaign notes
- Plain text entry, supporting Markdown syntax.
- Display as parsed Markdown
- Remember caret position between edits

## Save/Load
On closing program, save state of campaign (characters, their stats, current round and turn, dice rolling history, active theme)

On opening program, default to loading the previous campaign if one was loaded, otherwise ask the player if they want to create a new one or load an existing one.