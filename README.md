# PhasmoEditor

A local save editor for Phasmophobia on Windows x64.

**3.0.0-preview.1** · [Русская документация](docs/README.ru.md) · [Changelog](CHANGELOG.md)

## Preview status

Compatibility with the current game version is **not yet verified**.
Checks use synthetic fixtures based on the previous editor's save format.
Unsupported save structures are rejected before writing.

## Features

- A dark interface with rounded panels and a Phasmophobia-inspired palette.
- English by default; instant EN / RU switching without losing entered values.
- Automatic save discovery, including nested profile folders, and manual file selection.
- Current money, level and prestige values; review changes before saving.
- Separate backups for each write and restore, including access to legacy backups.
- In-memory JSON processing, verification before replacement and stale-file detection.
- The provided icon is used for the executable only.
- A GitHub link in the sidebar opens the author's profile.

## Download and use

Download a Windows x64 ZIP from [Releases](https://github.com/stth12/PhasmoEditor/releases)
when this preview is published. Extract it and run **PhasmoEditor.exe**.
The portable package includes .NET; no separate runtime installation is needed.

1. Close Phasmophobia and check Steam Cloud settings.
2. Click **Find** or **Open…** and select the save file.
3. Review the loaded values and edit the fields you need.
4. Click **Review and save**, then confirm the changes.
5. To undo a change, select a backup in **Backup archive** and click **Restore backup**.

Automatic discovery searches
`%USERPROFILE%\\AppData\\LocalLow\\Kinetic Games\\Phasmophobia`, including subfolders.

Backups are stored in **PhasmoEditorBackups** beside the selected save.
They are never automatically deleted. Restoring also backs up the current file.
A damaged save can be restored from a valid backup.
Legacy **SaveFileBackup.txt** files are also listed.

Language preferences are stored in `%LOCALAPPDATA%\\PhasmoEditor\\settings.json`.

## Build and verify

Requirements: Windows x64, PowerShell and .NET SDK 10.

~~~powershell
./scripts/prepare-release.ps1
~~~

The script builds the solution, runs the verification program, publishes a
self-contained executable, and creates a ZIP and SHA-256 checksum under
`artifacts/release`.

To build or verify separately:

~~~powershell
dotnet build PhasmoEditorWinApp.sln -c Release
dotnet run --project PhasmoEditor.Tests -c Release
~~~

GitHub Actions runs the same preparation script and uploads the package.
It does not publish a GitHub Release automatically.

## Scope and limitations

The editor retains the previous AES-CBC / PBKDF2-SHA1 save codec and requires
numeric `value` entries for `PlayersMoney`, `NewLevel` and `Prestige`.
Other JSON fields are preserved by value.

Input is limited to whole numbers between 0 and 2147483647.
Game-specific limits and relationships between progression fields still need verification.
The application does not manage Steam Cloud.

The replacement uses `File.Replace` and preserves the previous file in a backup.
A file changed since loading is rejected. A short race remains between the
change check and replacement: keep the game closed and avoid concurrent editors.
Game-process detection checks the name `Phasmophobia`.

## Repository layout

- `PhasmoEditorWinApp`: WPF application, localization and save handling.
- `PhasmoEditor.Tests`: executable verification suite.
- `scripts`: reproducible preview packaging.
- `docs`: Russian documentation, publishing steps and release notes.

See [Publishing](docs/PUBLISHING.md) for uploading this update to the existing repository.
