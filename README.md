<p align="center">
  <img src="docs/images/banner.png" alt="PhasmoEditor — Your progress. Your control." width="1280">
</p>

<p align="center">
  A local save editor for <strong>Phasmophobia</strong>.<br>
  A calmer workspace for your money, level and prestige.
</p>

<p align="center">
  <a href="https://github.com/stth12/PhasmoEditor/releases"><strong>Releases</strong></a>
  &nbsp; · &nbsp; <a href="#quick-start">Quick start</a>
  &nbsp; · &nbsp; <a href="docs/README.ru.md">Русский</a>
  &nbsp; · &nbsp; <a href="CHANGELOG.md">Changelog</a>
</p>

---

> **Tested on Phasmophobia v0.19.1.0.** The author reports that all application functions work correctly.
> Automated checks also cover synthetic save fixtures. Unsupported structures are rejected before writing.
> The application release remains **3.0.0-preview.1**; other game versions require separate verification.

## A closer look

<p align="center">
  <img src="docs/images/interface-en.png" alt="English interface with save selection, profile settings and backup archive" width="1120">
</p>

*Actual application interface. Screenshots use synthetic demonstration data.*

| Find your save | Adjust your profile | Keep a way back |
| :--- | :--- | :--- |
| Search nested profile folders or open a file manually. | Load current values and review changes before writing. | Keep separate backups and restore an earlier version. |

## Made for a simpler workflow

- **One focused window.** Rounded panels, restrained typography and a dark green palette.
- **English / Русский.** English on first launch; switch instantly without losing entered values.
- **Portable on Windows x64.** The release ZIP includes .NET. Extract it and run.
- **A backup for every operation.** Saving and restoring both preserve the previous file.
- **Checks before writing.** Verify the edited document and reject stale or unsupported saves.
- **Your files stay local.** Editing is performed on the selected file; Steam Cloud is not managed.

<details>
<summary><strong>See the Russian interface</strong></summary>

<p align="center">
  <img src="docs/images/interface-ru.png" alt="Russian interface with EN / RU language switching" width="1120">
</p>

The language preference is kept between launches. [Документация на русском](docs/README.ru.md).

</details>

## Quick start

**Download → Extract → Open → Review → Save**

1. Get **PhasmoEditor-3.0.0-preview.1-win-x64.zip** from the
   [preview release](https://github.com/stth12/PhasmoEditor/releases/tag/v3.0.0-preview.1) once published.
2. Extract the ZIP and run **PhasmoEditor.exe**.
3. Close Phasmophobia and check your Steam Cloud settings.
4. Click **Find** or **Open…**, then select the save you want to edit.
5. Adjust the values, click **Review and save**, and confirm the changes.

No separate .NET runtime installation is required for the portable release.

### Backups and recovery

Backups appear in **Backup archive**. Select one and click **Restore backup** to recover it.
The current file will also be backed up. A damaged save can be restored from a valid copy.

| Location | Purpose |
| :--- | :--- |
| `%USERPROFILE%\AppData\LocalLow\Kinetic Games\Phasmophobia` | Automatic discovery, including subfolders |
| `PhasmoEditorBackups` beside the selected save | Backup history; copies are never automatically deleted |
| `%LOCALAPPDATA%\PhasmoEditor\settings.json` | Saved EN / RU preference |

Legacy **SaveFileBackup.txt** copies are also listed.

## Build from source

Windows, PowerShell and **.NET SDK 10** are required.

~~~powershell
./scripts/prepare-release.ps1
~~~

This builds the solution, runs verification, and creates a self-contained ZIP plus
a SHA-256 checksum under `artifacts/release`.

<details>
<summary>Individual build commands and repository layout</summary>

~~~powershell
dotnet build PhasmoEditorWinApp.sln -c Release
dotnet run --project PhasmoEditor.Tests -c Release
~~~

| Folder | Contents |
| :--- | :--- |
| `PhasmoEditorWinApp` | WPF application, localization and save handling |
| `PhasmoEditor.Tests` | Executable verification suite and demo screenshot rendering |
| `scripts` | Preview packaging |
| `docs` | Images, Russian documentation and publishing notes |

GitHub Actions runs the same packaging script and uploads the ZIP and checksum.
It does not publish a Release automatically.

</details>

## Compatibility and limits

This preview retains the previous AES-CBC / PBKDF2-SHA1 codec.
The save must contain numeric `value` entries for
`PlayersMoney`, `NewLevel` and `Prestige`. Other JSON fields are preserved by value.

Input accepts whole numbers from **0 to 2147483647**.
Game-specific limits and relationships between progression fields still require verification.
Keep the game closed and avoid concurrent save editors: a short race remains between
the file-change check and replacement. Game-process detection uses the name `Phasmophobia`.

---

Created by **[stth12](https://github.com/stth12)** ·
[Report a problem](https://github.com/stth12/PhasmoEditor/issues) ·
[Publishing notes](docs/PUBLISHING.md)

*Independent community project. Not affiliated with the developers of Phasmophobia.*
