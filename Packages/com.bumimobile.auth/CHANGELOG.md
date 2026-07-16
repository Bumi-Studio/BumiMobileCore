# Changelog

All notable changes to this package are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/).

## [0.3.2] - 2026-07-16

### Fixed

- `IsCredentialAlreadyInUseError` now recognizes additional Firebase error patterns: "already linked", "already in use", "already exists", "different sign-in credentials", "different user account", and "another account" — preventing false negatives when Google credential reuse is reported with alternative phrasing.

## [0.3.1] - 2026-07-03

### Added

- `SyncFromFirebaseCurrentUser()` — syncs the cached `User` from Firebase's native current user, firing `OnFirebaseAuthChanged` only when user identity actually changes.
- Persistent Firebase auth state listener — attached once per app session, keeping `AuthService.User` in sync with Firebase's native `AuthStateChanged` events.
- `CancellationToken` support on all async auth methods (`SignInAsync`, `SignInAutoAsync`, `GoogleSignInAuthenticateAsync`, `TryAuthFirebaseWithGoogleAsync`, `CreateAnonymousAsync`) — accepts an optional token and propagates `OperationCanceledException`.

### Changed

- `AuthenticatedInitModule` startup timeout now uses `CancellationTokenSource.CancelAfter` instead of `UniTask.WhenAny`. On timeout, it calls `SyncFromFirebaseCurrentUser()` to capture any auth state resolved in-flight.
- All Firebase and Google Sign-In async calls now route through `.AsUniTask().AttachExternalCancellation(cancellationToken)` for proper cancellation integration.

## [0.3.0] - 2026-07-01

### Changed

- **Sign-out leaves the app signed out**: `SignOutAsync` no longer creates a fresh anonymous account after sign-out. `OnFirebaseAuthChanged` now fires only once (with `null`) instead of twice. This reverses the 0.2.2 behavior where the app always had a Firebase user. Code that relied on `User != null` after sign-out must now check `IsSignedIn`.
- **Sign-in respects explicit sign-out**: `SignInAsync` skips automatic anonymous sign-in when the player has explicitly signed out, preventing unwanted re-authentication on cold start.
- `AuthService.Initialize` is now idempotent when called with the same WebClientId, avoiding redundant Google Sign-In reconfiguration.

### Added

- `DEFAULT_GOOGLE_WEB_CLIENT_ID` constant on `AuthService` — the default OAuth Web Client ID provided by the framework.
- `IsExplicitlySignedOut` static property on `AuthService` for checking the sign-out flag.
- `EnsureGoogleConfigured()` method caches the `GoogleSignInConfiguration` per WebClientId, reducing allocations and configuration errors.
- `AuthenticatedInitModule.googleWebClientId` — serialized Inspector field for configuring the Google Web Client ID, defaulting to `DEFAULT_GOOGLE_WEB_CLIENT_ID`.
- `AuthenticatedInitModule.ConfigureAuthService()` — initializes `AuthService` before sign-in begins.
- Firebase-stub `SignOutAsync` now records the explicit sign-out flag locally instead of logging a warning.

### Fixed

- Google Sign-In configuration no longer throws when called before dependencies are ready; `EnsureGoogleConfigured` returns `false` gracefully.
- `SignOutAsync` Firebase sign-out now guarded with `EnsureGoogleConfigured()` to avoid errors when Google Sign-In package is not present.

## [0.2.2] - 2026-05-29

### Changed

- **Google Sign-In-first**: `SignInAsync` (auto-start) now attempts silent Google Sign-In before falling back to Firebase restore or anonymous. Previously it skipped Google entirely on auto-start.
- `SignOutAsync` now recreates an anonymous Firebase account immediately after sign-out, so the app always has a Firebase user (analytics, cloud save, etc. expect one).
- Removed `AuthType` enum and `CurrentAuthType` property — derive auth type from `User.IsAnonymous` instead.
- Removed `PREF_AUTH_TYPE` PlayerPrefs key — Firebase SDK persists the auth session natively.
- Removed `_googleUser` field — Firebase `User.DisplayName`/`Email` mirrors the Google profile after linking.
- `GetUserLabel()` simplified to use Firebase `User` directly.
- Reduced PlayerPrefs persistence to only `PREF_PLAYER_ID` (Google Sign-In user ID, not cached by Firebase).

### Fixed

- Handle "credential already in use" error during `ManualSignInAsync`: when signing in with Google and the credential belongs to a different Firebase account, sign into that existing account instead of failing. Added `IsCredentialAlreadyInUseError` helper that recurses into `InnerException`.
- `GetUserLabel()` now returns `"Guest {uid_prefix}"` for anonymous users instead of an empty string.
- `CreateAnonymousAsync` now fires `OnPlatformAuthFinished(false)` to signal platform auth was skipped.
- Added explicit-sign-out gate: if the user signs out via `SignOutAsync`, the next cold start skips silent Google Sign-In to prevent unwanted auto-re-authentication.
- `WebClientId` setter is now `private`; use `AuthService.Initialize(webClientId)` to set it once.
- `OnPgsAuthFinished` is now marked `[Obsolete]` with an auto-forwarding wrapper to `OnPlatformAuthFinished`.

### Breaking

- **`SignInAsync` no longer accepts `forceRefreshToken`** — the parameter was unused and has been removed.
- **`SignOutAsync` behavior change**: After sign-out, `User` is no longer `null` — a fresh anonymous `FirebaseUser` is created immediately. `OnFirebaseAuthChanged` fires with `null` (sign-out) then with the new anonymous user (re-auth). Code checking `User == null` after sign-out should use `IsSignedIn` instead.
- **`SignInWithCredentialAsync` on existing non-anonymous users**: If `auth.CurrentUser` is non-null and non-anonymous, calling `SignInWithCredentialAsync` creates a **new Firebase user with a different UID**, orphaning cloud data tied to the old UID. This occurs when a user switches Google accounts. Consider prompting the user before this path.
- **`WebClientId` is now `private set`** — must be set via `AuthService.Initialize(webClientId)`.

### Added

- `AuthService.Initialize(string webClientId)` — sets the OAuth Web Client ID once before any sign-in call.
- Silent Google Sign-In is now gated behind a `PlayerPrefs` flag (`__auth_explicitly_signed_out__`) that is set by `SignOutAsync` and cleared on successful sign-in.

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
