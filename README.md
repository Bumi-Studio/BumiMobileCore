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

| Package                          | What it covers                                                                                                                                                         |
| -------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Core** (`com.bumimobile.core`) | Base inspectors, tween utilities, initializer helpers, custom editor styles. Installed by default.                                                                     |
| **Auth** (`com.bumimobile.auth`) | Anonymous-first Firebase Auth — auto-restores or creates an anonymous session, upgrades to Google-linked on manual sign-in. |
| **Audio**                        | Lightweight BGM/SFX routing, init module, and editor config.                                                                                                           |
| **Currency**                     | Currency definitions, balances, and formatters.                                                                                                                        |
| **Defines**                      | Shared scripting define presets and toggles.                                                                                                                           |
| **Event Calendar** (`com.bumimobile.eventcalendar`) | Local-only seasonal scheduling with UTC ranges, active/upcoming/ended event queries, sprite swapping for UI and SpriteRenderer targets, cosmetic gating helpers, debug override support, and editor authoring tools. |
| **FullSerializer** (`com.bumimobile.fullserializer`) | Battle-tested JSON serializer with Unity type support, cyclic reference handling, and compact/pretty JSON output for save systems and data persistence. |
| **Haptic**                       | Haptic feedback abstraction for iOS & Android.                                                                                                                         |
| **Localization** (`com.bumimobile.localization`) | Multi-language support (22 languages) with CSV-based translation management, runtime language switching, Arabic text shaping, and Unity UI/TextMeshPro integration. |
| **Monetization**                 | Ads + IAP settings (with tabbed inspector) and runtime glue.                                                                                                           |
| **NativeShare**                  | Mobile share sheets for screenshots/text.                                                                                                                              |
| **Pool**                         | Object pooling service and helpers.                                                                                                                                    |
| **Push Notification**            | Mobile push initialization hooks.                                                                                                                                      |
| **Reward**                       | Daily/event reward data structures.                                                                                                                                    |
| **Save**                         | Save-system wrapper (PlayerPrefs + serializers).                                                                                                                       |
| **Security** (`com.bumimobile.security`) | AES-256 encryption with HMAC verification for tamper-resistant save data, per-install secret generation, and secure key derivation via PBKDF2. |
| **Skins**                        | Unlockable skin manager and sample UI.                                                                                                                                 |
| **Leaderboard** (`com.bumimobile.leaderboard`) | Firebase-backed leaderboards (global, regional, country aggregates) with caching, save integration, and warmup helpers.                                             |
| **UI**                           | Common popups, loading views, toasts, etc.                                                                                                                             |
| **Utilities**                    | Extra helpers shared by multiple modules.                                                                                                                              |

> Tween, inspector extensions, and initializer logic now live inside Core, so they no longer appear as standalone packages.

## Spotlight: Event Calendar

`com.bumimobile.eventcalendar` is designed for local seasonal events and lightweight live-ops style content without backend dependencies. It can:

- resolve one active event from UTC schedule data during project initialization,
- expose event phase queries for gameplay, UI, and cosmetic unlock logic,
- swap themed sprites on `Image` and `SpriteRenderer` targets with automatic fallback to defaults,
- support local debug forcing in editor and development builds,
- and provide a single inspector-driven authoring workflow for event assets, sprite libraries, validation, and overlap checks.

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

### SDK-backed modules

Some packages require external SDKs that Unity will not install automatically:

- **Auth** — Install Firebase Core/Auth (`com.google.firebase.app`, `com.google.firebase.auth`). For the "Sign in with Google" button, also install Google Sign-In (`com.google.signin.google-signin-unity`) and set `AuthService.WebClientId`. The package auto-enables `BUMI_AUTH_HAS_FIREBASE` and `BUMI_AUTH_HAS_GOOGLE_SIGNIN` when those SDKs are present.
- **Leaderboard** — Requires Firebase Core/Auth plus Firebase Firestore (`com.google.firebase.firestore`). When Firestore is present the package auto-enables `BUMI_LEADERBOARD_HAS_FIRESTORE`; otherwise it runs in offline stub mode.

---

## 🧰 Core Tools

### Project Setup

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
