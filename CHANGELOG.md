# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),  
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2025-06-11

### Added
- **Typewriter Text Effect**: `PriosTextLocalizer` now supports displaying text character-by-character, including:
  - Full **rich text and nested tag preservation** during animation.
  - **Timing variation** for punctuation (e.g. commas and periods create longer pauses).
  - **Audio feedback**: Optional character sounds with randomized clip selection and pitch control.
  - **Speed-up support**: Holding or pressing continue during typing speeds up animation (`speedUpMultiplier`).
  - Supports multi-line display with initial batch typing and per-line pagination (`scrollOneLineAtATime`).
- **Public Methods**:
  - `SetKeyAndShow(string key)`: Replaces the key and resets state for clean replay.
  - `Continue()`: Advances one line or speeds up current typing.
  - `IsTyping`, `IsComplete`, and `HasMoreText` properties for easy state queries.
- **Auto-Linked Debug Menu**: `PriosLinkData` is now included in the package, enabling `PriosDebugMenu` to appear automatically in builds.

### Changed
- **Rich Text Engine Overhaul**:
  - Text is now parsed and sliced using TMP's mesh data to preserve rich formatting correctly across lines.
  - Unclosed rich text tags are automatically closed at line breaks.
- **Editor Preview**:
  - `PriosTextLocalizer` truncates text visually in the editor based on TMP bounds and margin settings.
- **UI Improvements**:
  - Inspector now shows context-sensitive options based on effect usage.
  - Editor UI better reflects actual in-game text state.
- **Internal Refactoring**:
  - Removed redundant typewriter routines.
  - Improved coroutine control and line tracking logic for pagination and animation accuracy.
  - Robust placeholder watching with automatic updates when data keys or language change.


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
