# Changelog

All notable changes to this package are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/).

## [Unreleased]

- (Add unreleased changes here)

## [0.1.1] - 2025-12-01

### Added

- Identifier-based `AudioClips` catalog with grouped entries, making it easier to add new clips without editing code.
- `AudioClipReference` struct and `AudioController.TryPlaySound` helpers for resolving clips by id.

### Changed

- Legacy `buttonSound` now resolves to the `ui/button` entry in the catalog so existing code keeps working while new clips use the modular flow.

## [0.1.0] - 2025-11-14

### Added

- Initial release of the Audio package.
