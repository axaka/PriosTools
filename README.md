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
    - Text animates character-by-character with full **rich text support**, preserving all tags and formatting—even across word-wrapped/paginated lines.
    - **Only the newly revealed text animates**; existing visible text stays static.
    - Instantly skips and shows empty lines (empty lines do not consume a "continue" action in any scroll mode).
    - Auto-closes rich text tags at line breaks to avoid broken formatting.
    - Handles all pagination/scrolling modes: one-line-at-a-time and full-page, with correct tag context.
    - Honors pagination settings and editor preview sizing (`TruncateTextInEditor`), forces layout rebuilds under layout groups.
    - Supports per-character audio (randomized pitch/clip), and customizable punctuation pause timing.
    - Use `TryContinue()` (returns `bool`) for logic or `Continue()` (void) for UI binding.
    - Clean API: `SetKeyAndShow()`, `Continue()`, `IsTyping`, `IsComplete`, `HasMoreText`, etc.
    - **Supports ScriptableObject (SO) settings** for centralized, reusable localization configs.

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
