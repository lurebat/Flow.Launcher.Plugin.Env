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

- no arguments — List all user environment variables
- `KEY` - List the value of a specific variable, and copy it to the clipboard (e.g., `MY_VAR`)
- `add KEY VALUE` — Add or update a variable (e.g., `add MY_VAR hello`)
- `del/delete KEY` — Delete a variable (e.g., `delete MY_VAR`)
- `path` — List all PATH entries, each as a separate result, with a filter.
- `path KEY` — List PATH entries matching `KEY`, each as a separate result, copying the entry to the clipboard.
- `path add KEY` — Append `KEY` to the PATH variable.
- `path del/delete KEY` — Delete a PATH entry matching `KEY` (fuzzy search)

## License
MIT License
