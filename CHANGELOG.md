# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),  
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


## [1.3.3] - 2025-07-15

### Fixed
- Empty localization key now results in empty text instead of [Key]


## [1.3.2] - 2025-07-14

### Fixed
- **WebGL Compatibility:** Fixed missing `Refresh()` method errors in `PriosUserInputField` by ensuring the method is always defined, even outside the Unity Editor. This resolves build errors on WebGL and other non-Editor platforms.


## [1.3.1] - 2025-07-09

### Bugfix
- PriosEvent is now again a part of the PriosTools namespace


## [1.3.0] - 2025-07-09

### Added
- Generic overloads for `AddListener<T>`, `RemoveListener<T>`, and `TriggerEvent<T>` to support strongly-typed events in the `PriosEvent` system.
- Internal static `_wrappers` dictionary to map each original `Action<T>` callback to its `Action<object>` wrapper.


## [1.2.0] - 2025-07-09

### Improved
- **Rich Text Typewriter & Pagination**:  
  - Typewriter and pagination now handle **empty lines instantly**—empty lines are displayed immediately and do not consume a "continue" action in any scroll mode.
  - Internal routines now correctly combine a static prefix (previous lines) with only the *newly revealed* content for typewriter animation, ensuring only the new content types out while keeping already visible text static.
  - Unified to a single `RevealRichText(prefix, newContent, onFinish)` coroutine for all animation modes.
  - Rich text tag context is accurately reconstructed for every visible window, fixing prior issues with cut-off tags at the start of lines and ensuring all formatting is preserved regardless of line breaks or pagination.

### Changed
- **Continue Behavior**:  
  - All modes now **skip over empty lines automatically**, so only visible non-empty lines trigger the typewriter/page effect or consume a continue click.
- **Settings as ScriptableObject**:  
  - `TextLocalizerSettings` can now be used as a **ScriptableObject (SO)**, allowing you to share and edit localization settings across multiple scenes or components via the Unity asset system.

### Fixed
- **Tag Handling**:  
  - Eliminated issues where tags were dropped from the start of lines or caused visual glitches when lines began with a closing tag (e.g., missing `<b>` at the start of the first visible line).
  - Corrected bugs with rich tag context when typewriter and pagination interacted (especially on scrolling and page turns).
- **Scrolling**:  
  - Prevented unwanted extra lines from being counted due to tags being split by TMP word wrapping.


## [1.1.3] - 2025-07-04

### Improved
- **Text Display**: Refactored `PriosTextLocalizer` to always show the full text when both typewriter effect and pagination are disabled by short-circuiting the `UpdateText()` path.
- **Editor Preview**: Updated `TruncateTextInEditor()` to honor the `enablePagination` flag and force a proper layout rebuild (`Canvas.ForceUpdateCanvases()` + `LayoutRebuilder.ForceRebuildLayoutImmediate`) so sizing is correct under a `VerticalLayoutGroup`.
- **Typewriter Effect**: Guarded the coroutine logic to only run in Play mode, preventing “one character” previews when exiting Play.
- **Continue Control**: Introduced `TryContinue()` (returns `bool`) for core logic and a `public void Continue()` wrapper for Unity Button binding.
- **Cleanup**: Removed the unnecessary one-frame delayed `UpdateText()` coroutine now that layout rebuild is forced.


## [1.1.2] - 2025-06-11

### Bugfix
- **SetKeyAndShow**:
  - Cleans the state properly and UpdateText() doesn't run more than once even if SetKeyAndShow is ran more than once.


## [1.1.1] - 2025-06-11

### Improved
- **Key Selector UI**:
  - Replaced dropdown with a **searchable autocomplete input field** for selecting localization keys.
  - Suggestions update live as you type, making it much faster to navigate large key sets.
  - **Current selection is hidden** from suggestions to reduce clutter.
  - Input and label are now displayed **inline** for a cleaner layout.
  - Includes a `"— None —"` option to clear the key assignment.


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
