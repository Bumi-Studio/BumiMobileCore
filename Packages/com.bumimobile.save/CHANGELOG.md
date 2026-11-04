# Changelog

All notable changes to this package are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- `SaveController.TrySaveLocal(out error)` saves every module to the local file synchronously and never uploads. It returns `true` only after a wrapper that supports durable writes has written a detached snapshot of the save. Call it on the Unity thread.
- `SaveController.TrySaveLocalAndQueueCloud(out error)` makes the same local save and queues that snapshot for cloud upload, which stays best effort.
- `BaseSaveWrapper.SupportsDurableLocalSave` and `BaseSaveWrapper.TrySaveLocal(...)`. The default, Firebase, disabled-Firebase and Android wrappers support durable writes. WebGL still saves normally but reports no durable checkpoint.
- `BaseSaveWrapper.BeginCloudLoadChecked(onLoaded)` reports whether the cloud read succeeded, so a failed read is not mistaken for "no cloud save". `FirebaseSaveWrapper` implements it.
- `AtomicFile.Write(path, contents, createOnly)`, `GlobalSave.CreateSnapshot(updateLastExitTime)`, `SaveInitModule.UseDevCloudSave` and `WebGLSaveWrapper.LastLoadFailed`.

### Changed

- Local saves are written to a temporary file, flushed, then swapped in for the old file. Where `File.Replace` is unavailable, the swap goes through a backup that is put back if the move fails. A failed write never deletes the last good save, and errors are reported instead of swallowed. The binary save format is unchanged.
- Autosaves capture each module's data on the Unity thread. Local writes run in order and stale ones are dropped, so a delayed autosave cannot overwrite a newer save. Replacing, resetting or deleting the save cancels queued writes. Cloud uploads send the snapshot that was saved locally.
- `SaveController.SaveAndWaitForCloudAsync` always saves locally before uploading.
- `AndroidSaveWrapper.SaveLocal` no longer also uploads to the cloud.
- A save file that cannot be read is moved aside as `<file>.corrupt-…` and a new save starts. If it cannot be moved aside, loading throws instead of overwriting it. A missing file still starts a new save.

## [0.2.2] - 2026-11-04

### Added

- `BaseSaveWrapper.DeleteCloudSavesAsync()` for account deletion. `FirebaseSaveWrapper` deletes the current account's cloud save and this device's guest cloud save in one atomic batch and drops any queued upload; it never deletes the shared `device-fallback` document. Other wrappers return `false` because they cannot reach the cloud.

## [0.2.1] - 2026-08-14

### Fixed

- **Cloud saves now survive uninstall + reinstall for anonymous players.** The Firestore document key was the Firebase anonymous UID, which is deleted on uninstall — so reinstalling (e.g. forced reinstall after a failed Play Store update) created a new UID and the cloud save was unreachable. Anonymous players are now keyed by a stable device identifier (`SystemInfo.deviceUniqueIdentifier`; ANDROID_ID / identifierForVendor), which persists across uninstall. Google-linked players keep UID keying, so saves remain portable across devices. Encryption salts follow the same key, so existing Google-linked docs stay decryptable.

## [0.2.0] - 2026-07-01

### Added

- **Cloud save now API**: `SaveCloudNow(GlobalSave)` and `SaveCloudNowAsync(GlobalSave)` on `BaseSaveWrapper` (and `FirebaseSaveWrapper`) for immediate, non-queued cloud uploads.
- `SaveController.SaveAndWaitForCloudAsync(forceSave)` — saves locally then awaits a guaranteed cloud upload before resolving. Useful for critical save points (e.g., before scene transitions).
- **Local-vs-cloud conflict resolution**: When loading from the cloud, `SaveController` now compares the local save timestamp against the cloud save and keeps whichever is newer. If local is newer, it syncs the local data up to the cloud instead of overwriting.
- `FirebaseSaveWrapper.PerformUploadAsync()` — extracted upload logic from the background loop for direct call-sites.
- `saveCloudImmediately` and `saveCloud` parameters on `SaveController.Save()` for controlling cloud save behavior per call.

### Changed

- `BaseSaveWrapper.Save()` is now split into `SaveLocal()` and `SaveCloudNow()`. The original `Save()` still exists for backward compatibility and delegates to `SaveLocal()`.
- `SaveController.Save()` now delegates to `SaveLocal()` internally and manages cloud uploads via the new parameters, allowing callers to opt out of cloud saves or force immediate uploads.
- Firebase cloud save initialization respects `AuthService.IsExplicitlySignedOut` — if the player has explicitly signed out, cloud save is disabled and anonymous sign-in is skipped.
- `SaveController` now imports `System.Threading.Tasks` for async cloud save support.

### Fixed

- Cloud upload no longer silently overwrites a newer local save when loading cloud data; local vs cloud timestamp comparison prevents data loss.
- Firebase save wrapper handles explicit sign-out state gracefully without throwing or retrying initialization.

## [0.1.4] - 2026-06-29

### Added

- Added `saveSlotIndex` field to `SaveInitModule` and `SaveController` to support multiple save slots. When set to `1` or higher, the save file is named `save_1`, `save_2`, etc., allowing separate save files per reset or per device.

## [0.1.3] - 2026-04-29

### Added

- Added a `SaveInitModule` option to route Firebase cloud saves to `players_saves_dev` or `players_saves_prod`.

### Changed

- Changed Firebase cloud saves to write one encrypted player save document instead of one document per save service, reducing Firestore write quota usage.

## [0.1.2] - 2025-12-02

### Added

- Integrated `com.bumimobile.security` and `com.bumimobile.fullserializer` directly into the Save package so cloud backends no longer rely on Unity Visual Scripting assemblies.
- Encrypted Google Play Games and Firebase cloud payloads using `SaveCrypto`, ensuring user data stays encrypted at rest.

### Changed

- Updated `BumiMobile.Save.asmdef` references and scripting defines so Firebase (`BUMI_SAVE_CLOUD_FIREBASE`) and Google Play Games (`BUMI_SAVE_CLOUD_GPGS`) code only compiles when the respective packages are installed.
- Switched serializer pipelines to the new `FullSerializer` namespace and UTF-8 encoding for cloud traffic.

## [0.1.0] - 2025-11-14

### Added

- Initial release of the Save package.
