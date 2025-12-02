# Changelog

All notable changes to this package are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/).

## [Unreleased]

- (Add unreleased changes here)

## [0.1.3] - 2025-12-02

### Changed

- Allow the runtime asmdef to reference `GoogleMobileAds`, `GoogleMobileAds.Core`, and `GoogleMobileAds.Ump` so AdMob code resolves the moment the plugin is installed.
- Added matching references (including `GoogleMobileAds.Editor`) to the editor asmdef so AdMob inspectors/build hooks compile as soon as the SDK is present.

## [0.1.2] - 2025-12-01

### Added

- README documenting monetization features, providers, and setup guidance.

### Changed

- `BumiMobile.Monetization.asmdef` now adds `MODULE_ADMOB`, `MODULE_LEVELPLAY`, and `MODULE_UNITYADS` automatically when their packages are present.

## [0.1.0] - 2025-11-14

### Added

- Initial release of the Monetization package.
