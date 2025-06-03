# PriosTools

**PriosTools** is a modular Unity package that streamlines game development with tools for live data integration, localization, events, save systems, UI animation, and debugging — all plug-and-play ready.

---

## ✨ Features

- **📊 Live Data Loading**
  - Load JSON, CSV, or Google Sheets.
  - Auto-generate C# classes.
  - Supports runtime reloading — no need to rebuild for content fixes.

- **🌍 Localization System**
  - Spreadsheet-based translations.
  - Dynamic placeholders (e.g., player name).

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
