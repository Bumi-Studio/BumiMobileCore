# Event Calendar Basic Demo

This sample includes one local event database and a few copied `Paskah` sprites from `SuperBoltsPuzzle`.

## Included Assets

- `Data/EventCalendarDatabase.asset`
- `Data/Event_paskah_sample.asset`
- `Data/EventSpriteLibrary_paskah_sample.asset`
- a few sprites under `Sprites/`

## How to Try It

1. Import the sample into your Unity project.
2. Add the **Event Calendar** module to your `Project Init Settings`.
3. Assign the sample database.
4. Add `EventSpriteSwapController` to any UI or scene object.
5. Use one of these target ids in the controller:
   - `sample_avatar`
   - `sample_board`
   - `sample_background`
6. Create matching `Image` or `SpriteRenderer` targets and enter Play Mode.

The sample event is configured with a broad UTC date range so it is active by default in development builds.
