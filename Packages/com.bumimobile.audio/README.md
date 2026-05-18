# Bumi Mobile Audio

Audio playback, pooling, and configuration helpers used across Bumi Mobile projects. The package provides a data-driven `AudioLibrary` workflow for organising sound effects, a `SoundKey` struct for type-safe clip references, pooled `AudioSource` utilities, and an init-module that keeps setup consistent between games.

## Features

- `AudioLibrary` ScriptableObject with a **Group → Item** model – add, remove, and reorder clips in the Inspector without editing code.
- `SoundKey` struct for serialising grouped clip references (`Group` + `Id`) inside prefabs and ScriptableObjects.
- `SoundKeyDrawer` custom property drawer – Group and Id dropdowns are auto-populated from the `AudioLibrary` in Resources.
- `AudioRefsGenerator` editor tool – auto-generates a strongly-typed `AudioKeys` class (e.g. `AudioKeys.UI.Button`) so you never hard-code string identifiers.
- `AudioController` static facade with pooled `AudioSource`s, default 2D/3D settings, clip lookup via `SoundKey` or `"Group/Id"` strings, and `TryPlaySound` shortcuts.
- `AudioInitModule` that plugs into the Core initializer to bootstrap the listener, library, and source pool at startup.
- Volume routing per `AudioType` with `VolumeChanged` callback for UI sliders.

## Requirements

- Unity 2021.3 or newer.
- `com.bumimobile.core` (the initializer, module registry, and shared utilities live there).

## Getting Started

1. **Create an AudioLibrary catalog**

   - Project window ▸ Create ▸ _Audio ▸ Audio Library (Data-Driven)_.
   - Add groups (UI, Game, Music, etc.) and items with unique string IDs such as `Button`, `Win`, `Explosion_Big`.
   - Place the asset in a **Resources** folder and name it `AudioLibrary` so the `SoundKeyDrawer` can locate it automatically.

2. **Add the Audio Init Module (recommended)**

   - Open your `ProjectInitSettings` asset (from `com.bumimobile.core`).
   - Click **Add Module** and select **Audio Controller**.
   - Assign the `AudioLibrary` catalog and configure the pool size plus optional 3D defaults (max distance, spread, rolloff curve).
   - The editor auto-creates an `AudioLibrary` asset when you first add the module.

3. **Manual initialization (alternative)**

```csharp
[SerializeField] private AudioLibrary library;
[SerializeField] private int poolSize = 8;

void Awake()
{
    AudioController.OverrideDefault3DAudioSettings(30f, 180f, AnimationCurve.EaseInOut(0, 1, 1, 0));
    AudioController.Init(library, poolSize);
}
```

## Using SoundKey References

`SoundKey` is a serialisable struct with `Group` and `Id` fields. In the Inspector it renders as two dropdowns populated from the `AudioLibrary` in Resources.

```csharp
[SerializeField] private SoundKey rewardSound;

public void OnRewardGranted()
{
    if (AudioController.TryPlaySound(rewardSound))
    {
        // Sound played successfully
    }
}
```

## Using the Catalog at Runtime

- Fetch clips by string key (supports `"Group/Id"` format and bare `Id` fallback):

```csharp
AudioController.TryPlaySound("UI/Button");
AudioController.TryPlaySound("UI/Button", position, 0.8f, 1.1f);
```

- Fetch clips by `SoundKey`:

```csharp
var key = new SoundKey("UI", "Button");
AudioController.TryPlaySound(key);
```

- Resolve a clip directly without playing:

```csharp
var clip = AudioController.GetClip("UI/Button");
var clip = AudioController.GetClip(new SoundKey("FX", "Explosion"));
```

## Strongly-Typed Audio Keys (Auto-Generated)

The `AudioRefsGenerator` produces a `AudioKeys.g.cs` file containing nested static classes that mirror your `AudioLibrary` groups and IDs:

```csharp
// Auto-generated: AudioKeys.UI.Button => new SoundKey("UI", "Button")
AudioController.TryPlaySound(AudioKeys.UI.Button);
AudioController.TryPlaySound(AudioKeys.Game.Win, position);
```

The file is regenerated automatically whenever an `AudioLibrary` asset changes. You can also generate it manually via **Tools ▸ Audio ▸ Generate Audio Keys**.

## Volume Control

- `AudioController.SetVolume(AudioType.Sound, sliderValue);` updates SFX levels.
- Subscribe to `AudioController.VolumeChanged` for UI feedback:
  ```csharp
  AudioController.VolumeChanged += (type, volume) => { /* update slider */ };
  ```

## Troubleshooting

- `TryPlaySound` logs a warning if the clip key is missing in the library. Keep Group/Id pairs unique to avoid silent failures.
- If nothing plays, verify the Audio Init Module ran (check the `Initializer` prefab) or ensure `AudioController.Init` was called manually before any playback requests.
- The listener is auto-created when the controller initialises. Use `AudioController.AttachAudioListener` to parent it to a camera or character rig.
- If `SoundKeyDrawer` dropdowns appear empty, confirm an `AudioLibrary` asset exists inside a **Resources** folder.

## Migration from 0.1.x

The 0.2.0 release replaces several legacy types:

| Old (0.1.x)            | New (0.2.0)      | Notes |
|-------------------------|-------------------|-------|
| `AudioClips` / `MD_AudioClips` | `AudioLibrary` | Data model changed to Group → Item |
| `AudioClipReference`   | `SoundKey`        | Two-field struct instead of single string |
| `AudioCase`            | `SoundKey`        | Use `AudioController.TryPlaySound(key)` |
| `AudioSourceCase`      | Internal `PooledSource` | Managed automatically by `AudioController` |
| `AudioClipHandler`     | —                 | Use `SoundKey` + `AudioController` directly |
| `AudioSave`            | —                 | Volume persistence left to consumers |
| `MusicSource`          | —                 | Removed; use a dedicated `AudioSource` for music |

The `AudioController.buttonSound` property still resolves to `"UI/Button"` in the new library for backwards compatibility.

## License

See the repository root `LICENSE` file for licensing terms.