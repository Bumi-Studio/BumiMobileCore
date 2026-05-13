# Changelog

All notable changes to this package are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/).

## [Unreleased]

- _Nothing yet._

## [0.2.1] - 2026-05-13

### Changed

- **Anonymous-first**: `SignInAsync` (auto-start) no longer attempts Google Sign-In at all. It goes straight to Firebase — restoring any persisted session (linked or anonymous), or creating a new anonymous account. Google Sign-In is now **only** triggered by `ManualSignInAsync` (the "Sign in with Google" button).
- `InternalSignInAsync` split into two distinct paths:
  - `manual=false` → `EnsureFirebaseAnonIfPossibleAsync` (restore / anonymous).
  - `manual=true` → Google Sign-In → `LinkWithCredentialAsync` (upgrade anonymous to linked).
- Updated XML doc comments on `SignInAsync` and `InternalSignInAsync` to accurately describe the anonymous-first flow.
- `AuthenticatedInitModule` log message updated (no longer mentions Play Games).

## [0.2.0] - 2026-05-13

### Changed

- **BREAKING**: Replaced Google Play Games (GPGS) platform sign-in with **Google Sign-In** Unity package (`com.google.signin.google-signin-unity`).
  - `AuthService` now uses `GoogleSignIn` / `GoogleAuthProvider` instead of `PlayGamesPlatform` / `PlayGamesAuthProvider`.
  - Google Sign-In uses **ID tokens** (not server auth codes), so the `forceRefreshToken` parameter on `SignInAsync` is no longer consumed (kept for API compatibility).
  - Platform sign-in is now cross-platform (Android + iOS), not Android-only.
  - `AuthService.WebClientId` must be configured before calling `ManualSignInAsync`.
  - `OnPgsAuthFinished` renamed to `OnPlatformAuthFinished`.
  - Assembly definition: version define `BUMI_AUTH_HAS_GPGS` replaced with `BUMI_AUTH_HAS_GOOGLE_SIGNIN`.
- `GetUserLabel()` now prefers Google Sign-In display name/email over Firebase.
- `SignOutAsync` now calls `GoogleSignIn.DefaultInstance.SignOut()` in addition to Firebase sign-out.

### Removed

- All Google Play Games (GPGS) code paths: `EnsurePgsActivated`, `PgsAuthenticateAsync`, `RequestPgsServerAuthCodeAsync`, `TryLoginOrLinkFirebaseWithPgsAsync`.
- Assembly reference `Google.Play.Games`.

## [0.1.4] - 2025-12-08

### Changed

- `AuthenticatedInitModule` now participates in the core async initialization pipeline, preventing duplicate sign-in attempts and guaranteeing the initializer waits for a definitive Play Games success/failure (falling back to anonymous when needed).
- Sign-in no longer aborts immediately when the optional timeout elapses; instead it logs the delay and keeps waiting for the actual Play Games result to avoid false negatives.

## [0.1.2] - 2025-11-21

### Added

- Optional `BUMI_AUTH_HAS_FIREBASE` compile flag so the package can compile in stub mode when Firebase SDK is not yet installed.
- Automatic version-defined scripting symbols for Firebase (`BUMI_AUTH_HAS_FIREBASE`) and Google Play Games (`BUMI_AUTH_HAS_GPGS`) when their Unity packages are present.
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
