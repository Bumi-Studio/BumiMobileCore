# Changelog

All notable changes to this package are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/).

## [Unreleased]

- (Add unreleased changes here)

## [0.1.1] - 2025-12-03

### Added

- `BumiMobile.FullSerializer.Editor` asmdef that references `UnityEditor` so editor-only types such as `fsAotConfigurationEditor` compile only in the editor.

## [0.1.0] - 2025-12-02

### Added

- Initial extraction of FullSerializer into a standalone `com.bumimobile.fullserializer` UPM package.
- Unity-ready asmdefs so other Bumi packages (`Save`, `Security`, etc.) can depend on `FullSerializer` types without pulling Visual Scripting.
