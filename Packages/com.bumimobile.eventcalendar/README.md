# Bumi Mobile Event Calendar

Bumi Mobile Event Calendar is a local-only seasonal content package for Unity projects. It lets you define event windows in UTC, resolve one active event during initialization, query event state from gameplay code, and swap themed sprites on `Image` or `SpriteRenderer` targets without hard-coding event visuals into scenes.

## What You Can Use

- `EventCalendarInitModule` to plug the package into the Bumi Mobile Core initializer flow.
- `EventCalendarDatabase` as the main asset that stores all registered events.
- `EventCalendarData` to define an `EventId`, `DisplayName`, optional UTC schedule, and linked sprite library.
- `EventSpriteLibrary` to map `TargetId` values to themed sprites.
- `EventCalendarService` to read the active event, query event phases, and fetch themed sprites at runtime.
- `EventSpriteSwapController` to replace sprites on UI `Image` or world-space `SpriteRenderer` targets.
- `EventCosmeticAvailabilityResolver` to hide, unlock, or keep event cosmetics locked based on event phase and ownership.
- Custom inspectors for event creation, id generation, schedule editing, validation, and sprite library authoring.
- A `Basic Demo` sample with ready-to-test assets and target ids.

## Feature Summary

- Local-only event scheduling with no backend, Remote Config, or live service dependency.
- UTC schedule formatting, parsing, and validation through `EventCalendarScheduleUtils`.
- Single active event resolution at startup with overlap detection and safe fallback to default visuals.
- Local debug override support through `forceEventId` in the init module for editor and development builds.
- Phase-based queries for `Upcoming`, `Active`, `Ended`, `Missing`, and `Invalid` event states.
- Automatic sprite fallback when no event is active or when a target id has no event sprite.
- Optional target hiding and native-size image resizing for event sprites.
- Two visual update modes:
  - `Live` keeps the controller in sync with the current runtime state.
  - `FreezeAfterStartupVisual` locks the first startup visual for the lifetime of the controller.
- Validation for duplicate event ids, duplicate target ids, invalid UTC ranges, missing sprite libraries, and overlapping schedules.

## Requirements

- Unity `2021.3` or newer.
- `com.bumimobile.core`.
- `com.unity.ugui` when you use UI `Image` targets.

## Installation

Add the package to `Packages/manifest.json` and pin the Git tag or branch you want to consume:

```json
{
  "dependencies": {
    "com.bumimobile.core": "https://github.com/Bumi-Studio/BumiMobileCore.git?path=Packages/com.bumimobile.core#<tag-or-branch>",
    "com.bumimobile.eventcalendar": "https://github.com/Bumi-Studio/BumiMobileCore.git?path=Packages/com.bumimobile.eventcalendar#<tag-or-branch>"
  }
}
```

Keep `Packages/packages-lock.json` under version control so the team resolves the same package revisions.

## Quick Start

1. Create an `EventCalendarDatabase` asset from `Create > Data > Event Calendar Database`.
2. Open `Project Init Settings` and add the **Event Calendar** module.
3. Assign the database to the module.
4. Use the database inspector to create an event. The editor can auto-create the paired `EventSpriteLibrary`.
5. Set a lowercase `EventId` that uses only letters, numbers, and underscores.
6. Enable `Use Schedule` and enter `Start UTC` / `End UTC` when the event should activate automatically.
7. Add sprite entries to the library and match each `TargetId` with the target ids used in your scene or prefab controllers.
8. Add `EventSpriteSwapController` to any object that should react to the active event.
9. Add controller targets, assign `Image` or `SpriteRenderer` references, capture default sprites, and enter Play Mode.

## Authoring Workflow

The recommended authoring flow lives inside the `EventCalendarDatabase` inspector:

1. Click `Create Event` to generate a new `EventCalendarData` asset and matching `EventSpriteLibrary`.
2. Edit the display name, event id, sprite library, and schedule from the same inspector.
3. Use the built-in validation warnings to catch missing assets, invalid ids, bad UTC values, and schedule overlaps early.
4. Open the sprite library inline and maintain the `TargetId -> Sprite` mappings without leaving the authoring hub.
5. Add or remove existing event assets from the database without deleting the asset files themselves.

By default, generated assets are created under the project data folder:

- `Assets/Project Files/Data/Event Calendar/EventData`
- `Assets/Project Files/Data/Event Calendar/EventLibraries`

## Runtime API

### Core Types

- `EventCalendarService.CurrentState` exposes the currently resolved `EventCalendarRuntimeState`.
- `EventCalendarService.TryGetActiveEventData(out EventCalendarData eventData)` returns the active event asset, if any.
- `EventCalendarService.TryGetActiveEventSprite(string targetId, out Sprite sprite)` resolves a themed sprite by target id.
- `EventCalendarService.GetEventSpriteOrDefault(string targetId, Sprite defaultSprite = null)` returns a themed sprite or a fallback.
- `EventCalendarService.GetEventQuery(string eventId)` returns `EventCalendarEventQuery` with the event phase, asset reference, and validation message.

### Event Phases

`GetEventQuery` can return these phases:

- `None` for an empty event id.
- `Upcoming` when the schedule exists but the event has not started yet.
- `Active` when the event is currently live or forced by local debug override.
- `Ended` when the schedule has passed.
- `Missing` when the event id is not present in the database.
- `Invalid` when the event exists but its schedule is not valid.

### Cosmetic Gating

`EventCosmeticAvailabilityResolver.Resolve(...)` helps gate event cosmetics:

- `Available` when the event is active, or after it ends when the item is owned or the post-event mode allows it.
- `LockedEventOnly` when the event has ended and the item should stay locked unless previously owned.
- `Hidden` when the event is upcoming, missing, or invalid.

Example:

```csharp
using BumiMobile;
using UnityEngine;

public class EventCalendarExample : MonoBehaviour
{
    [SerializeField] private string cosmeticEventId = "ramadan_2026";
    [SerializeField] private bool isOwned;

    private void Start()
    {
        if (EventCalendarService.TryGetActiveEventData(out EventCalendarData activeEvent))
            Debug.Log($"Active event: {activeEvent.DisplayName}");

        EventCalendarEventQuery query = EventCalendarService.GetEventQuery(cosmeticEventId);
        Debug.Log($"Event phase: {query.Phase}");

        EventCosmeticAvailability availability = EventCosmeticAvailabilityResolver.Resolve(
            "Ramadan Skin",
            cosmeticEventId,
            EventCosmeticPostEventMode.Lock,
            isOwned);

        Debug.Log($"Cosmetic availability: {availability}");
    }
}
```

## Sprite Swap Workflow

Each `EventSpriteSwapController` contains one or more targets:

- Assign either a UI `Image` or a `SpriteRenderer` per target.
- Set the same `TargetId` used in the active event's `EventSpriteLibrary`.
- Use `Capture All Defaults` or `Capture` to store the non-event state.
- Enable `Hide When Inactive` when the target should disappear outside the event window.
- Keep `Use Native Size For Event Sprite` enabled for UI images that should resize to the incoming sprite's native size.

If a target has no matching event sprite, the controller restores the captured default sprite and active state.

## Schedule Rules

- Schedules use UTC and are stored in the `yyyy-MM-ddTHH:mm:ssZ` format.
- The runtime only activates events with a valid schedule where `End UTC >= Start UTC`.
- Only one scheduled event can be active at a time.
- If multiple events overlap at runtime, the package logs a warning and falls back to default visuals.
- Local debug overrides are available only in the Unity Editor or development builds.

## Sample

`Samples~/Basic Demo` includes:

- one sample `EventCalendarDatabase`,
- one sample `EventCalendarData`,
- one sample `EventSpriteLibrary`,
- demo sprites,
- and three ready-to-test target ids:
  - `sample_avatar`
  - `sample_board`
  - `sample_background`

## License

See the repository root `LICENSE` file for licensing terms.
