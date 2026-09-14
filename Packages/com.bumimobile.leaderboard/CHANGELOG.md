# Changelog

## [0.1.1] - 2025-11-25

### Added

- `LeaderboardInitModule` that initializes the leaderboard system during project bootstrap.
- Auth-aware initialization: only runs if `AuthenticatedInitModule.IsAuthenticated` is true.
- Optional warmup with configurable top-limit.

## [0.1.0] - 2025-11-24

### Added

- Initial release of `com.bumimobile.leaderboard`.
- Firestore-backed repository with optional dummy fallback.
- Save-service integration and warmup loading task.
- Documented setup requirements.
