# Bumi Mobile Audio

Audio playback, pooling, and configuration helpers used across Bumi Mobile projects. The package provides a catalog-driven workflow for organizing sound effects, pooled playback utilities, and init-module glue that keeps setup consistent between games.

## Features

- Identifier-based `AudioClips` catalog with grouped slots, automatic lookup cache, and backwards-compatible button click support.
- `AudioClipReference` struct for serializing catalog references without hard-coding `AudioClip` assets inside prefabs.
- `AudioController` static facade with pooled `AudioSource`s, default 2D/3D settings, clip lookup helpers, and `TryPlaySound` shortcuts.
- Optional `AudioInitModule` that plugs into the Core initializer to bootstrap the listener, catalog, and source pool at startup.
- Volume routing per `AudioType`, persistence via the Save module (when `MODULE_SAVE` is available), and change callbacks for UI sliders.
- Utilities such as `AudioClipHandler`, `MusicSource`, and tween-friendly fade helpers for advanced UX flows.

## Requirements

- Unity 2021.3 or newer.
- `com.bumimobile.core` (the initializer, module registry, and shared utilities live there).
- Optional: `MODULE_SAVE` define plus the Save module if you want `AudioController` volumes to persist between sessions.

## Getting Started

1. **Create an `AudioClips` catalog**

   - Project window ▸ Create ▸ _Audio Clips_ (searchable via the asset menu). Populate groups (UI, Gameplay, etc.) and add slots with unique identifiers such as `ui/button` or `fx/explosion_big`.
   - The legacy `buttonSound` field resolves to the `ui/button` slot so existing references keep working.

2. **Add the Audio Init Module (recommended)**

   - Open your `ProjectInitSettings` asset (from `com.bumimobile.core`).
   - Click **Add Module** and select **Audio Controller**.
   - Assign the `AudioClips` catalog and configure the pool size plus optional 3D defaults (max distance, spread, rolloff curve).
   - The initializer now bootstraps `AudioController` before the rest of your gameplay systems run.

3. **Manual initialization (alternative)**

```csharp
[SerializeField] private AudioClips catalog;
[SerializeField] private int poolSize = 8;

void Awake()
{
    AudioController.OverrideDefault3DAudioSettings(30f, 180f, AnimationCurve.EaseInOut(0, 1, 1, 0));
    AudioController.Init(catalog, poolSize);
}
```

## Using the Catalog at Runtime

- Fetch clips directly: `var clip = AudioController.GetClip("ui/button");`
- Fire-and-forget playback with validation:

```csharp
AudioController.TryPlaySound("ui/button");
AudioController.TryPlaySound("fx/explosion_small", transform.position, 0.8f, 1.1f);
```

- Serialize safe references inside prefabs or scriptable objects:

```csharp
[SerializeField] private AudioClipReference rewardSound;

public void OnRewardGranted()
{
    if (rewardSound.TryResolve(out var clip))
    {
        AudioController.PlaySound(clip);
    }
}
```

- Iterate every slot for tooling/integration:

```csharp
foreach (var slot in audioClips.EnumerateSlots())
{
    Debug.Log($"{slot.Id} -> {slot.DisplayName}");
}
```

## Volume, Music, and Advanced Playback

- Call `AudioController.SetVolume(AudioType.Sound, sliderValue);` to update SFX levels; subscribe to `AudioController.VolumeChanged` for UI feedback.
- When the Save module is present the current volumes are serialized via `AudioSave`, so `SetVolume` automatically flags data for persistence.
- Add a `MusicSource` to scene singletons. Call `musicSource.Init()` once, optionally `SetAsDefault()`/`Activate()` to control cross-fading between multiple theme tracks.
- Use `AudioClipHandler` components for designer-friendly knobs: it supports cooldowns, dynamic pitch stepping, dedicated `AudioSource`s, and tween-friendly fades.

## Troubleshooting

- `TryPlaySound` logs a warning if the clip identifier is missing. Keep identifiers unique per catalog slot to avoid silent failures.
- If nothing plays, verify the Audio Init Module ran (check the `Initializer` prefab) or ensure `AudioController.Init` was called manually before any playback requests.
- The listener is auto-created when the controller initializes. Use `AudioController.AttachAudioListener` to parent it to a camera/character rig when needed.

## License

See the repository root `LICENSE` file for licensing terms.
