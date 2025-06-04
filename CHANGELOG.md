# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),  
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2025-06-05

### Added
- **Typewriter Text Effect**: `PriosTextLocalizer` now supports displaying text character-by-character, with:
  - Rich text and nested tag support.
  - Optional character sound effects with randomized pitch and clip selection.
  - Public `FinishTyping()` method to instantly show the full text.
- **Auto-Linked Debug Menu**: `PriosLinkData` is now included in the package, enabling the `PriosDebugMenu` to appear automatically in builds without manual setup.

### Changed
- Updated editor UI to support typewriter and sound settings in `PriosTextLocalizerEditor`.
- Refactored and extended core data loading and UI components to support typewriter and audio features.

## [1.0.0] - 2025-06-02

### Added
- **Google Spreadsheet Integration**: Download and parse data from Google Sheets, JSON, or CSV.
- **Runtime Data Reloading**: Update external data without rebuilding the project.
- **Class Generator**: Automatically generate typed C# classes from spreadsheet schema.
- **Localization System**: Load translations from Google Sheets with support for dynamic variables (e.g., player name).
- **Custom Event System**: Decouple logic using PriosEvents, PriosEventTrigger, and PriosEventListener.
- **SaveGame System**: Save/load system with Unity Editor UI.
- **UI Animation Tools**: Enter/exit animation helpers for UI elements.
- **Debug Menu**: Access save data and scene management live in build via `F9`.
- **PriosSingleton**: Simple base class to implement singletons across managers.

### Notes
- This is the initial public release of PriosTools.
- Designed for easy integration into any Unity project.
