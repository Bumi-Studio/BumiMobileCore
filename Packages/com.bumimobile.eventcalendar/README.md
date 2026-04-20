# Bumi Mobile Event Calendar

Local-only seasonal event support for Bumi Mobile projects. The package lets a project define dated events in UTC, resolve which event is active at startup, and swap sprites across UI or world-space renderers without hard-coding theme assets into scenes.

## Features

- `EventCalendarInitModule` integration for the Core initializer flow.
- `EventCalendarDatabase` and `EventCalendarData` assets for authoring local event schedules.
- `EventSpriteLibrary` assets that map target ids to local `Sprite` references.
- `EventSpriteSwapController` for swapping `Image` or `SpriteRenderer` sprites at runtime.
- Editor tooling for creating events, validating schedules, and editing sprite libraries from a single authoring hub.
- Optional `EventCosmeticAvailabilityResolver` helper for gating cosmetics by event lifecycle.

## Requirements

- Unity 2021.3 or newer.
- `com.bumimobile.core`.
- `com.unity.ugui` if you use `Image` targets.

## Getting Started

1. Create an `EventCalendarDatabase` asset from `Create > Data > Event Calendar Database`.
2. Open your `Project Init Settings` asset.
3. Add the **Event Calendar** module.
4. Assign the database to the module.
5. Create one or more `EventCalendarData` assets from the database inspector.
6. For each event, assign a sprite library and set a UTC start/end range.
7. Add `EventSpriteSwapController` to any prefab or scene object that should react to the active event.
8. Add target ids in the controller and match those ids inside the event sprite library.

## Authoring Workflow

- Use the custom `EventCalendarDatabase` inspector as the main authoring surface.
- Create events from the inspector and let it auto-create a matching sprite library.
- Keep event ids lowercase with numbers or underscores only.
- Avoid overlapping UTC ranges. The runtime falls back to the default visuals if more than one event is active.

## Runtime Behavior

- At init, the package evaluates the active local event using `DateTime.UtcNow`.
- If exactly one valid event is active, the runtime exposes that event through `EventCalendarService`.
- `EventSpriteSwapController` instances apply event sprites when available and fall back to their captured defaults when no event is active.

## Samples

The package ships with a small sample set under `Samples~/Basic Demo` that includes:

- a sample database,
- one sample event,
- one sample sprite library,
- and a few example sprites copied from `SuperBoltsPuzzle`.

## License

See the repository root `LICENSE` file for licensing terms.
