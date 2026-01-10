# Windows File Mover

A lightweight WPF utility for Windows that scans a selected folder, lists matching files, and lets you move checked files to a destination folder with optional folder-structure preservation and automatic renaming when conflicts occur.

The UI exposes quick filters (preset extensions, keyword, and custom extension list), large selection checkboxes, and progress/status indicators so you always know what is happening.

> ?? This tool performs file moves immediately and without safeguards. Use it only when you understand the impact and accept full responsibility for the moved files.

## Key Features

- `Pick Source folder…` / `Pick Destination…` selectors with status labels.
- Search filtration by keyword plus built-in toggles for common media extensions (mp4, mkv, avi, mov, wmv, flv, webm) and a custom text box for additional extensions.
- Large checkbox column for the file list, with dedicated `Select all` / `Select none` actions and `Move selected` that activates only when files are selected.
- Progress bar and status text that update while scanning and moving files.
- Optional behaviors: maintain the source folder structure inside the destination and auto-rename conflicting files.

## How It Works

1. Choose a source folder; the app recursively searches for files with the selected extensions and optional keyword.
2. Choose the destination folder where selected files will be moved.
3. Use the checkboxes (or select-all/none) to choose which files to move. The `Move selected` button becomes enabled when at least one file is checked.
4. Click `Move selected` to copy the files. Progress is displayed in the bar and status text.
5. Optional behaviors:
   - Keep folder structure (from source): re-creates the relative subfolders inside the destination.
   - Auto-rename on conflict moving: appends numeric suffixes when a file already exists at the target path.

## Usage Tips

- Use the keyword and custom extensions to narrow results in large media folders.
- If you need to re-select, click `Select all` and `Select none` to reset the checkboxes before toggling specific files.
- Watch for the `Move selected` button’s enabled state; it is driven by the view model and updates as soon as the `IsSelected` flag changes.

## Requirements

- Windows
- .NET 9 (`net9.0-windows`)
- WPF support via Visual Studio, Rider, or the `dotnet` CLI

## Build

```bash
dotnet build WindowsFileMover/WindowsFileMover.csproj
```

## Security & Safety

There are no built-in confirmations, dry runs, backups, or undo steps. The application immediately moves files from the source to the destination and may overwrite or delete files depending on the options. You operate it at your own risk.

## Roadmap Ideas

- Preview window showing selected files before moving.
- Move history / undo stack.
- Configurable filter presets for common media sets.
- Drag-and-drop source/destination paths.

## License

Open source under the MIT License. See `LICENSE` for details.