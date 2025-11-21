# Bumi Mobile Auth

Authentication foundation for Bumi Mobile projects. This package wraps platform sign-in (Google Play Games) with Firebase authentication, provides country bootstrap utilities, and exposes an initialization module that plugs into the Core initializer pipeline.

## Features

- `AuthenticatedInitModule` — an init module that signs players in during bootstrap with timeout handling and skip/disable flags persisted via `PlayerPrefs`.
- `AuthService` — static helper for platform login (Google Play Games v2) linked to Firebase Auth with anonymous fallbacks.
- `CountryService` — resolves the player country via IP lookup, caches the result, and optionally provides flag sprites via `CountryFlagDatabase` ScriptableObject.
- `CountryFlagDatabase` — editable asset mapping ISO country codes to flag sprites and localized names.

## Getting Started

1. **Install dependencies**

   - Add Firebase Core/Auth (`com.google.firebase.app`, `com.google.firebase.auth`), Google Play Games v2 (`com.google.play.games`), and [UniTask](https://github.com/Cysharp/UniTask) (`com.cysharp.unitask`) via the Unity Package Manager or by extending your project `manifest.json`.
   - Android builds require Google Play Games; iOS builds only consume the Firebase Auth dependency at runtime.

2. **Enable scripting define symbols**

   - Add `BUMI_AUTH_HAS_FIREBASE` (Player Settings ▸ Scripting Define Symbols) after importing the Firebase SDK. Without this flag the runtime falls back to a stub implementation.
   - Add `BUMI_AUTH_HAS_GPGS` after importing the Google Play Games SDK to enable platform sign-in. Leave it undefined to skip Play Games integration.

3. **Create the Authenticated Init Module**

   - Use `Create ▸ BumiMobile ▸ Auth ▸ Authenticated Init Module` to create an asset.
   - (Optional) Assign a `CountryFlagDatabase` for flag lookups.
   - Add the module to your `ProjectInitSettings` asset so it runs before other modules.

4. **Configure skip/disable flags (optional)**

   - `PlayerPrefs` key `__auth_disabled__` permanently disables auto sign-in when set to `1`.
   - `PlayerPrefs` key `__reset_skip_auth__` skips the next sign-in attempt (cleared automatically).

5. **Using Country Flags**

   - Create a `CountryFlagDatabase` asset and populate ISO codes with sprites and display names.
   - Assign it to the init module so the service can resolve display labels and sprites.

6. **Profile Save Integration (optional)**
   - Define `BUMIMOBILE_PROFILE_SAVE` to enable the helper method `CountryService.CaptureLocationOnceAsync(ProfileSave)` for saving location snapshots.

## Folder Layout

- `Runtime/` — runtime scripts and assembly definition.
- `CHANGELOG.md` — version history following Keep a Changelog.
- `package.json` — Unity package manifest.

## License

See the repository root `LICENSE` file for terms.
