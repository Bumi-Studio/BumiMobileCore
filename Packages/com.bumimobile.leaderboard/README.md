# Bumi Mobile Leaderboard

Server-backed leaderboard utilities (global, regional, and country aggregates) built for Bumi Mobile Core.

## Features
- Reusable `LeaderboardController` with caching, warmup, and submit helpers.
- Firestore repository that syncs scores, regions, and per-country totals.
- Save-system hook (`LeaderboardSaveService`) to keep pending progress offline.
- Optional loading-task warmup for the Core initializer pipeline.

## Requirements
- Install **Firebase Core/Auth** and **Firebase Firestore** via the Unity Package Manager or Embedded packages.
- Add the scripting define symbol `BUMI_LEADERBOARD_HAS_FIRESTORE` (automatically set when `com.google.firebase.firestore` is present thanks to the assembly definition version define).
- Ensure `AuthService` has signed in (anonymous is fine) before submitting scores.
- `SaveController` must be initialized because the leaderboard saves piggy-back on it.

## Usage
```csharp
LeaderboardController.Init();
await LeaderboardController.SubmitScore(1500);
var globalTop10 = await LeaderboardController.GetCachedAsync(LeaderboardType.Global, 10);
```

For custom backends, call `LeaderboardController.SetRepository(yourImplementation)` before `Init()`.
