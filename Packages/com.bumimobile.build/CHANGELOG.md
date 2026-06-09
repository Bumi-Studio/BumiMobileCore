# Changelog

All notable changes to this package are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/).

## [0.2.0] - 2026-06-09

### Changed

- Replaced the custom Internal Test, Cheat, and Production presets with Android Build Profile selection.
- Build type, APK/AAB format, Player Settings, and scripting defines now come from the selected profile.
- Builds now run through `BuildPipeline.BuildPlayer(BuildPlayerWithProfileOptions)`.
- Version and publishing settings are saved to the selected Build Profile asset.
- Keystore passwords are stored per developer and per Build Profile.
- Increased the minimum supported Unity version to 6000.0.

### Removed

- Removed configurable preset labels and the custom cheat scripting define.

## [0.1.0] - 2026-06-08

### Added

- Android pre-build confirmation for version, bundle version code, and custom keystore settings.
- Configurable Internal Test, Cheat, and Production build presets.
- Team-shared project settings for preset labels, the cheat scripting define, and window enablement.
- Per-user, per-project persistence for the last selected build preset.
