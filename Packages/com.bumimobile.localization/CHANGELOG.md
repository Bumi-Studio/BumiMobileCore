# Changelog

All notable changes to this package are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/).

## [Unreleased]

- (Add unreleased changes here)

## [0.1.3] - 2025-12-11

### Added

- Added `BootstrapLanguage()` method with `RuntimeInitializeOnLoadMethod` attribute to load language settings before scene load
- Added `LoadLanguageFromPrefsOrAuto()` method to load language from PlayerPrefs or detect system language
- Added `MapSystemLanguage()` method to map Unity's `SystemLanguage` to `LanguageType` enum
- Added support for automatic language detection based on device system language

### Changed

- Updated `Language` property to persist changes to PlayerPrefs automatically
- Updated `Language` property setter to prevent unnecessary change events and log language changes
- Updated `AutoLanguage()` method to detect and use system language instead of defaulting to English
- Updated `Init()` method to check for existing preferences before loading

### Fixed

- Fixed language persistence issue where language would reset on game restart
- Fixed language not being available during early initialization

## [0.1.2] - 2025-12-08

### Fixed

- Fixed namespace reference in `LocalizationController.cs` for Arabic text support

## [0.1.1] - 2025-11-14

### Changed

- Minor updates and improvements

## [0.1.0] - 2025-11-14

### Added

- Initial release of the Localization package.
