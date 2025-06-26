# Flow.Launcher.Plugin.Env

A Flow Launcher plugin to manage Windows user environment variables directly from the Flow Launcher interface.

## Features
- List all user environment variables
- Add or update a user environment variable
- Delete a user environment variable
- Special handling for the PATH variable:
  - List all PATH entries
  - Append a new entry to PATH
  - Delete a PATH entry using fuzzy search

## Usage
Type `env` in Flow Launcher to activate the plugin, then use the following commands:

- `list` — Show all user environment variables
- `add KEY VALUE` — Add or update a variable (e.g., `add MY_VAR hello`)
- `del/delete KEY` — Delete a variable (e.g., `delete MY_VAR`)
- `path <something>` — List all PATH entries, each as a separate result, with a filter.
- `path add <something>` — Append `<something>` to the PATH variable.
- `path del/delete <something>` — Delete a PATH entry matching `<something>` (fuzzy search)

## License
MIT License
