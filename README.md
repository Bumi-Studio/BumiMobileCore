# 🎮 BumiMobileCore – Unity Modular Toolkit

Modular Unity packages for mobile & hyper-casual games. Install the core once, then mix and match feature packages directly inside the editor.

> Distributed as Unity packages (Git-based). No more manual `.unitypackage` imports.

---

## ✨ Highlights

- **Core-first architecture** – `com.bumimobile.core` ships editor tooling, inspectors, tween helpers, and shared utilities.
- **One-click package manager** – download/update modules without leaving Unity (`Window ▸ Bumi Mobile Core ▸ Package Manager`).
- **Plug-and-play modules** – install only what your game needs; every package is versioned and independently upgradable.
- **Project bootstrapper** – create folder structures, init scenes, and settings via `Prepare Project` in seconds.

---

## 📦 Module Overview

| Package | What it covers |
| --- | --- |
| **Core** (`com.bumimobile.core`) | Base inspectors, tween utilities, initializer helpers, custom editor styles. Installed by default. |
| **Audio** | Lightweight BGM/SFX routing, init module, and editor config. |
| **Currency** | Currency definitions, balances, and formatters. |
| **Defines** | Shared scripting define presets and toggles. |
| **Haptic** | Haptic feedback abstraction for iOS & Android. |
| **Localization** | String tables, runtime localization helpers, editor importers. |
| **Monetization** | Ads + IAP settings (with tabbed inspector) and runtime glue. |
| **NativeShare** | Mobile share sheets for screenshots/text. |
| **Pool** | Object pooling service and helpers. |
| **Push Notification** | Mobile push initialization hooks. |
| **Reward** | Daily/event reward data structures. |
| **Save** | Save-system wrapper (PlayerPrefs + serializers). |
| **Skins** | Unlockable skin manager and sample UI. |
| **UI** | Common popups, loading views, toasts, etc. |
| **Utilities** | Extra helpers shared by multiple modules. |

> Tween, inspector extensions, and initializer logic now live inside Core, so they no longer appear as standalone packages.

---

## 🚀 Installation

### Option A – Built-in Package Manager (Recommended)

1. Import `com.bumimobile.core` (embed or add via manifest as described below).
1. In Unity open `Window ▸ Bumi Mobile Core ▸ Package Manager`.
1. For each module:

    - Pick the desired tag/branch (e.g., `audio-v0.2.0`, `dev`, `main`).
    - Click `Install` or `Update`. The window uses Unity’s Package Manager API and pins entries inside `Packages/manifest.json`.

1. Already installed modules show their current version and can’t be re-downloaded until you change the target version.

### Option B – Manual manifest entry

Add Git dependencies directly in `Packages/manifest.json`:

```jsonc
{
  "dependencies": {
    "com.bumimobile.core": "https://github.com/Bumi-Studio/BumiMobileCore.git?path=Packages/com.bumimobile.core#core-v0.2.0",
    "com.bumimobile.audio": "https://github.com/Bumi-Studio/BumiMobileCore.git?path=Packages/com.bumimobile.audio#audio-v0.3.0",
    "com.bumimobile.ui": "https://github.com/Bumi-Studio/BumiMobileCore.git?path=Packages/com.bumimobile.ui#ui-v0.5.0"
  }
}
```

Save the file and Unity will resolve each package. Keep `Packages/packages-lock.json` under version control so teammates get identical revisions.

---

## 🧰 Core Tools

### Project Setup

- Menu: `Window ▸ Bumi Mobile Core ▸ Prepare Project`
- Generates the recommended folder layout, initializer prefab, and scenes.

```text
Assets/
├── BumiMobileCore/
└── ProjectFiles/
├── Data/
└── Game/
  ├── Animations/
  ├── Audio/
  ├── Fonts/
  ├── Images/
  ├── Materials/
  ├── Models/
  ├── Prefabs/
  ├── Scenes/
  ├── Scripts/
  ├── Shaders/
  └── Textures/

```

### Core Settings Asset

- Automatically stored at `Assets/Bumi Mobile Core/Core Settings.asset` (copied out of the package on first load).
- Use it to configure folders, initializer behavior, ads placeholders, etc.

---

## 📚 Documentation

- [GitHub Wiki](https://github.com/Bumi-Studio/BumiMobileCore/wiki)
- Each package ships with its own `Documentation~/` folder and changelog (`CHANGELOG.md`).

---

## 📝 License

MIT License – free for personal or commercial projects. Attribution appreciated but not required.

---

## 💬 Contact & Support

- Instagram: [@bumistudio.idn](https://www.instagram.com/bumistudio.idn/)
- Email: [mobile@bumistudio.co.id](mailto:mobile@bumistudio.co.id)
- Issues & feature requests: [GitHub Issues](https://github.com/Bumi-Studio/BumiMobileCore/issues)
