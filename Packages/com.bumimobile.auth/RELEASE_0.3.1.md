# com.bumimobile.auth v0.3.1

**Date:** 2026-07-03
**Type:** Patch — backward-compatible reliability & cancellation improvements

---

## What's new

### Firebase auth state listener
A persistent `AuthStateChanged` listener is now attached once per app session. `AuthService.User` stays in sync with Firebase's native auth state automatically — no more stale user references.

### `SyncFromFirebaseCurrentUser()`
New public method that synchronizes the cached `User` from Firebase's current user. Only fires `OnFirebaseAuthChanged` when the user identity actually changes (different UID or anonymous state).

### CancellationToken support
All async auth methods now accept an optional `CancellationToken`:

| Method | New signature |
|---|---|
| `SignInAsync` | `SignInAsync(CancellationToken ct = default)` |
| `SignInAutoAsync` | `SignInAutoAsync(CancellationToken ct = default)` |
| `CreateAnonymousAsync` | `CreateAnonymousAsync(CancellationToken ct = default)` |
| `GoogleSignInAuthenticateAsync` | `GoogleSignInAuthenticateAsync(bool interactive, CancellationToken ct = default)` |
| `TryAuthFirebaseWithGoogleAsync` | `TryAuthFirebaseWithGoogleAsync(GoogleSignInUser user, CancellationToken ct = default)` |

All Firebase and Google Sign-In async calls now route through `.AsUniTask().AttachExternalCancellation()`, and `OperationCanceledException` is properly propagated.

### Startup timeout refactor
`AuthenticatedInitModule` now uses `CancellationTokenSource.CancelAfter` instead of `UniTask.WhenAny` for its sign-in timeout. On timeout, it calls `SyncFromFirebaseCurrentUser()` to capture any auth state that resolved in-flight before unblocking the main menu.

---

## Migration guide

No breaking changes. Existing code continues to work — all new `CancellationToken` parameters are optional (`default`).

You can optionally adopt the cancellation token in your own callers:

```csharp
// Before
var success = await AuthService.SignInAsync();

// After — cancellable
var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromSeconds(10));
var success = await AuthService.SignInAsync(cts.Token);
```
