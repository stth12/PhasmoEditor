# Changelog

## 3.0.0-preview.1

### Added
- A new single-window design with rounded panels and a Phasmophobia-inspired palette.
- A sidebar link to the author's GitHub profile.
- English and Russian UI, instant switching and persistent language preferences.
- Recursive save discovery and manual file selection.
- Current-value display and a confirmation preview.
- Per-operation backups, restore, legacy backup discovery and backup-folder access.
- Validation for unsupported formats, invalid input and running game processes.
- Verification fixtures covering legacy decoding, untouched JSON fields, backup history,
  restore, corrupt files, stale snapshots, file locks and localization.
- Reproducible preview packaging and Windows CI.

### Changed
- Migrated from .NET Framework 4.8 to .NET 10 WPF.
- Replaced delete-and-move writes with File.Replace and backup creation.
- Moved save handling out of the window implementation.
- Removed global string replacements and decrypted temporary files.
- Kept the custom icon on the executable only.
- Removed unused legacy windows, resources and generated settings.

### Known limitation
Compatibility with the current Phasmophobia version is not yet verified.
This preview is tested against synthetic fixtures of the previous format.
