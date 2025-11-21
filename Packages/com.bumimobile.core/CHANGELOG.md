# Changelog

All notable changes to this package are documented in this file.

Note: Previous historical entries exist in `Documentation~/Core Changelog.md`.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/).

## [Unreleased]

- Added `com.bumimobile.auth` to the Core package manager window so the auth module can be installed like the other packages.

## [0.1.2] - 2025-11-20

### Added

- Optional EventSystem support toggle in `Initializer`, allowing projects to disable automatic input module wiring when using custom setups.

### Changed

- Documentation now consolidated into the package README for quicker onboarding.
- Disabling `Use Event System` now deactivates the referenced EventSystem and removes auto-wired input modules, preventing duplicate systems from spawning.
- UI helpers now respect the absence of an event system instead of implicitly creating one at runtime.

## [0.1.0] - 2025-11-14

### Added

- Initialize the unified changelog for the Core package.
