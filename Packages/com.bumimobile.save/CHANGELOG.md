# Changelog

All notable changes to this package are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/).

## [Unreleased]

- (Add unreleased changes here)

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
