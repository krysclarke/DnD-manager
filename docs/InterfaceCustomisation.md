# Interface customisation
Theme selection, UI scaling, and the web-interface settings all live in the **Settings** tab (which replaces the old "Other" tab).

## Themes
- ALL included themes must meet WCAG AA, and ideally WCAG AAA, contrast requirements.
- Switchable & customisable 'on-the-fly', all from the main program.
- The web interface defaults to match the main program's theme, but can be set to a different theme (see `WebInterface.md`).
- Display a warning message to the user if the contrast falls below WCAG AA, taking account of the current interface scale.

### Included themes
Eleven built-in themes ship with the app:
- system (default — follows the OS light/dark setting)
- high-contrast dark
- high-contrast light
- D&D 'parchment'
- 'arcane'
- purple
	- darkest color #1d1433
- 'forest' (dark, deep-green / druid)
- 'infernal' (dark, crimson / dragon-fire)
- 'dungeon' (dark, slate-steel / atmospheric stone)
- 'royal' (dark, gold / treasure)
- 'frost' (light, ice-teal — cold counterpart to parchment)

### Custom themes
- Users can create / clone / edit their own themes (colors and fonts) from the Settings tab.
- Custom themes are persisted (stored in the campaign database, see `Persistence.md`).

## UI sizing
- Allow the UI scale to be increased/decreased by button click and a slider (0.5x - 2.0x, in 0.25x steps).
- Allow the Web interface to be scaled separately from the main program (same 0.5x - 2.0x / 0.25x range).