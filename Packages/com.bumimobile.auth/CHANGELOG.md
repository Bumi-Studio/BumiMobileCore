# Changelog

All notable changes to this package are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/).

## [Unreleased]

- (Add unreleased changes here)

## [0.1.2] - 2025-11-21

### Added

- Optional `BUMI_AUTH_HAS_FIREBASE` compile flag so the package can compile in stub mode when Firebase SDK is not yet installed.
- Documented the `BUMI_AUTH_HAS_GPGS` guard for Google Play Games integration.

## [0.1.1] - 2025-11-20

### Fixed

- Declared Firebase Auth/App and Google Play Games v2 dependencies in the package manifest to avoid missing reference errors.

## [0.1.0] - 2025-11-20

### Added

- `AuthenticatedInitModule` integrating Auth bootstrap into the core initializer with timeout, skip, and disable flows.
- `AuthService` wrapping Google Play Games v2 + Firebase Auth sign-in, with anonymous fallback.
- `CountryService` and `CountryFlagDatabase` for resolving and displaying player country information.
- Assembly definition and package documentation.
