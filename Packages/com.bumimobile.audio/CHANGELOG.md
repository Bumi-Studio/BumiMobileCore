# Changelog

All notable changes to this package are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [0.2.0] - 2026-05-15

### Added

- `SoundKey` struct for serialisable grouped clip references (`Group` + `Id`), replacing the previous `AudioClipReference`.
- `AudioLibrary` ScriptableObject with a `Group → Item[]` model, runtime lookup dictionary, and `CreateAssetMenu` entry (*Audio ▸ Audio Library (Data-Driven)*).
- `SoundKeyDrawer` custom property drawer – Group and Id dropdowns auto-populated from the AudioLibrary in Resources.
- `AudioRefsGenerator` editor tool that produces a strongly-typed `AudioKeys` static class (e.g. `AudioKeys.UI.Button`) from all AudioLibrary assets. Regenerates automatically when libraries change, or on demand via *Tools ▸ Audio ▸ Generate Audio Keys*.
- `AudioController.GetClip(SoundKey)` and `TryPlaySound(SoundKey, …)` overloads for type-safe playback.
- `AudioController.GetClip(string)` now supports `"Group/Id"` slash format and falls back to searching all groups when no slash is present.
- Private `PooledSource` inner class replaces the former `AudioSourceCase`.

### Changed

- **Breaking:** Removed `AudioCase`, `AudioClipHandler`, `AudioClipReference`, `AudioSave`, `AudioSourceCase`, `MD_AudioClips`, and `MusicSource`. Migrate to `SoundKey` + `AudioLibrary`.
- `AudioInitModule` now references `AudioLibrary` instead of the old `AudioClips` catalog.
- `AudioInitModuleEditor` auto-creates an `AudioLibrary` asset when a new module is added.
- `AudioController` no longer depends on `AudioSave`; volume persistence is left to consumers.
- `AudioController.buttonSound` still resolves to `"UI/Button"` in the new library for backwards compatibility.

### Removed

- `AudioCase`, `AudioClipHandler`, `AudioClipReference`, `AudioSave`, `AudioSourceCase`, `MD_AudioClips`, `MusicSource`.

## [0.1.2] - 2025-12-01

### Added

- Identifier-based `AudioClips` catalog with grouped entries, making it easier to add new clips without editing code.
- `AudioClipReference` struct and `AudioController.TryPlaySound` helpers for resolving clips by id.

### Changed

- Legacy `buttonSound` now resolves to the `ui/button` entry in the catalog so existing code keeps working while new clips use the modular flow.

## [0.1.0] - 2025-11-14

### Added

- Initial release of the Audio package.
