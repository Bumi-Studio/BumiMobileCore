# Changelog

All notable changes to this package are documented in this file.

Note: Previous historical entries exist in `Documentation~/Core Changelog.md`.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/).

## [Unreleased]

- _Nothing yet._

## [0.1.4] - 2026-06-04

### Added

- Bumi Core Package Manager now resolves and installs missing `com.bumimobile.*` package dependencies before installing the selected package.
- Added recursive dependency ordering and circular dependency detection for Bumi package installs.

### Changed

- Bumi Core Package Manager now defaults package installs to the `dev` branch instead of `main`.
- External package dependencies remain manual and are no longer queued by the Bumi package dependency installer.

## [0.1.3] - 2025-12-04

### Added

- Initialization flow now surfaces progress via `Initializer.OnInitializationProgress`, `SetInitializationStatus`, and `ReportInitializationProgress`, enabling custom loaders to display ongoing module states.
- `GameLoading` automatically queues a core initialization task so progress text feeds into the existing loading UI without extra wiring.
- Introduced `GameLoading.SetLoadingStatus` to accept external progress percentages and feed them to loading UI listeners.

### Changed

- Game scene activation now waits until every init module (including async ones like Auth/Save) confirms completion, preventing premature transitions when services are still starting.
- `Initializer` keeps modules alive during loading, de-duplicating coroutine runs and ensuring status text always reflects the latest module in progress.
- `Initializer` now forwards module progress to the loading screen, keeping both text and percentage aligned while initialization runs.

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
