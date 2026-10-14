<p align="center">
  <img src="docs/images/banner.png" alt="PhasmoEditor 3.0.0" width="1280">
</p>

<p align="center">
  Save editor for <strong>Phasmophobia</strong> — money, level and prestige.
</p>

<p align="center">
  <a href="https://github.com/stth12/PhasmoEditor/releases/download/v3.0.0/PhasmoEditor-3.0.0-win-x64.exe"><strong>Download for Windows x64</strong></a>
  &nbsp; · &nbsp; <a href="docs/README.ru.md">Русский</a>
  &nbsp; · &nbsp; <a href="CHANGELOG.md">Changelog</a>
</p>

<p align="center"><strong>Version 3.0.0 · Tested on Phasmophobia v0.19.1.0 · EN / RU</strong></p>

## Features

- Edit money, level and prestige with current values loaded from your save.
- Find saves in nested profile folders or select a file manually.
- Review changes before saving.
- Keep a backup for every write and restore earlier copies.
- Switch between English and Russian without restarting.
- Run a single EXE — .NET is included.

<p align="center">
  <img src="docs/images/interface-en.png" alt="PhasmoEditor interface" width="1120">
</p>

## Getting started

1. Download **PhasmoEditor-3.0.0-win-x64.exe** from [Releases](https://github.com/stth12/PhasmoEditor/releases/tag/v3.0.0).
2. Close Phasmophobia and check Steam Cloud settings.
3. Run the EXE and click **Find** or **Open…**.
4. Select your save, change the values and click **Review and save**.

No installation or separate .NET runtime is required.

## Backups

Use **Backup archive → Restore backup** to return to an earlier save.
Copies are stored in **PhasmoEditorBackups** beside the selected file.
Restoring also backs up the current file. Legacy **SaveFileBackup.txt** copies are supported.

<details>
<summary><strong>Русский интерфейс</strong></summary>

<img src="docs/images/interface-ru.png" alt="PhasmoEditor — русский интерфейс" width="1120">

[Инструкция на русском](docs/README.ru.md).

</details>

## Build

Windows and .NET SDK 10 are required.

~~~powershell
./scripts/prepare-release.ps1
~~~

The script builds the application, runs checks and produces the standalone EXE
and SHA-256 checksum in `artifacts/release`.

---

**[stth12](https://github.com/stth12)** ·
[Report an issue](https://github.com/stth12/PhasmoEditor/issues) ·
[Releases](https://github.com/stth12/PhasmoEditor/releases)
