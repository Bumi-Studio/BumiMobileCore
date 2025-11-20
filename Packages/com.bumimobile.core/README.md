# Bumi Mobile Core

Base infrastructure used by Bumi Mobile projects. This package wires together initialization, module loading, and shared utilities so gameplay assemblies can focus on content.

## Features

- Project bootstrapper (`Initializer`) that persists across scenes and drives the startup pipeline.
- `ProjectInitSettings` asset orchestrating init modules in a defined order.
- Module registration system (`InitModule` + `[RegisterModule]`) to plug in optional systems.
- Utility helpers such as `InitializerContext`, coroutine runner proxies, and scene loading helpers.

## Getting Started

1. **Install the package**

   - Add `com.bumimobile.core` to your Unity project (git reference or scoped registry).

2. **Configure the Initializer prefab**

   - Place the `Initializer` prefab in your startup scene (or create a GameObject and add the `Initializer` component).
   - Assign your `ProjectInitSettings` asset.
   - Optionally assign an `EventSystem` and enable `Use Event System` to auto-wire the appropriate input module (Input System UI or Standalone).

3. **Create ProjectInitSettings**

   - Use `Create ▸ BumiMobile ▸ Core ▸ Project Init Settings`.
   - Add modules via the inspector; drag in assets or reference prefabs as required.

4. **Author Init Modules**

   - Derive from `InitModule` and annotate with `[RegisterModule]` to expose modules in the settings UI.
   - Override `CreateComponent` / `DestroyComponent` for setup and teardown.

5. **Runtime Scheduling Helpers**
   - Access `Initializer.GameObject`, `Initializer.Transform`, or `Initializer.RunCoroutine(...)` for shared lifetime management.

## Event System Toggle

The `Initializer` now exposes a `Use Event System` checkbox. When enabled and an `EventSystem` reference is provided, it adds:

- `InputSystemUIInputModule` if the Input System package define `MODULE_INPUT_SYSTEM` is present.
- Otherwise `StandaloneInputModule` (legacy Input Manager).

Disable the toggle if you manage input modules manually or are using UI frameworks that spin up their own event systems.

## License

Refer to the repository root `LICENSE` file for licensing terms.
