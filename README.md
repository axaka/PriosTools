# PriosTools

**PriosTools** is a modular Unity package that streamlines game development with tools for live data integration, localization, events, save systems, UI animation, and debugging — all plug-and-play ready.

---

## ✨ Features

- **📊 Live Data Loading**
  - Load JSON, CSV, or Google Sheets.
  - Auto-generate C# classes.
  - Supports runtime reloading — no need to rebuild for content fixes.

**🌍 Localization System**
  - Spreadsheet-based translations.
  - Dynamic placeholders (e.g., player name).
  - **Typewriter Effect** (fully featured):
  - Always shows full text when both typewriter and pagination are off.
  - Honor pagination flag even in the editor preview (`TruncateTextInEditor`).
  - Forces layout rebuild under layout groups so wrapping/pagination works immediately.
  - Displays text character-by-character with **rich text preservation**.
  - Public API:
  - `TryContinue()` returns success/failure and `Continue()` (void) for button binding.
  - Supports audio clips per character with **randomized pitch and clip selection**.
  - Customizable punctuation timing (e.g., commas and periods cause pauses).
  - User interaction with `Continue()` to advance lines or speed up typing.
  - Supports **pagination**, line wrapping, and bounds-based truncation.
  - Clean public API: `SetKeyAndShow()`, `Continue()`, `IsTyping`, `IsComplete`, etc.

- **⚡ Event System**
  - Decouple logic using `PriosEvents`, `PriosEventTrigger`, `PriosEventListener`.

- **💾 SaveGame System**
  - Full save/load solution with Editor tools.

- **🎭 UI Animation**
  - Smooth enter/exit transitions for UI elements.

- **🐞 Debug Menu**
  - Press `F9` in any scene (even in builds) to:
    - View/edit save data.
    - Switch scenes.
    - Integrated via `PriosLinkData`.

- **🔁 Singleton Helper**
  - Easy global access with `PriosSingleton`.

---

## 📦 Installation

Install via Unity's **Package Manager**:

1. Open `Edit > Project Settings > Package Manager`.
2. Under "Scoped Registries", make sure Git packages are allowed.
3. In your `manifest.json` (in `Packages/`), add:

```json
"dependencies": {
  "com.prios.tools": "https://github.com/axaka/PriosTools.git"
}
