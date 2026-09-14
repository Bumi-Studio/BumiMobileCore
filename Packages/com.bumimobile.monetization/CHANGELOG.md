# Changelog

All notable changes to this package are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/).

## [0.1.5] - 2026-05-15

### Added

- Sample package (`Samples~/Monetization`) registered in `package.json` so it is discoverable and installable through the Unity Package Manager Editor window.
- Documentation for installing the sample via the UPM Editor or manually from `Samples~`.

## [0.1.4] - 2025-12-15

### Added

- Adjusted ad size to be adaptive with full width device

## [0.1.3] - 2025-12-02

### Changed

- Allow the runtime asmdef to reference `GoogleMobileAds`, `GoogleMobileAds.Core`, and `GoogleMobileAds.Ump` so AdMob code resolves the moment the plugin is installed.
- Added matching references (including `GoogleMobileAds.Editor`) to the editor asmdef so AdMob inspectors/build hooks compile as soon as the SDK is present.
- Replaced direct `GoogleMobileAds.Editor` invocations with a reflection-based helper, avoiding hard assembly dependencies when the SDK omits those types.

## [0.1.2] - 2025-12-01

### Added

- README documenting monetization features, providers, and setup guidance.

### Changed

- `BumiMobile.Monetization.asmdef` now adds `MODULE_ADMOB`, `MODULE_LEVELPLAY`, and `MODULE_UNITYADS` automatically when their packages are present.

## [0.1.0] - 2025-11-14

### Added

- Initial release of the Monetization package.
