# PhasmoEditor 3.0 preview: redesigned UI and reliable save handling

The previous editor relied on a fixed save path and deleted the original before
moving its replacement into place. This update adds nested-profile discovery and
manual selection, verifies the edited document before writing, and uses File.Replace
with a separate backup for each operation.

The WPF application now has a single-window design, instant EN / RU switching
with English as the default, persistent language preferences, backup recovery,
an executable-only icon and a link to the author's GitHub profile.
The project moves to .NET 10 and produces a self-contained Windows x64 package.

Validation: Release build without warnings, fixture checks for save handling and
localization, and local execution of the same packaging script used by CI.
The author subsequently tested all application functions on Phasmophobia v0.19.1.0 and reports that they work correctly.
