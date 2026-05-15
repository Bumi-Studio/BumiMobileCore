# Bumi Mobile Auth

Google Sign-In-first authentication for Bumi Mobile projects. This package attempts silent Google Sign-In at app start (restoring returning users seamlessly), falls back to restoring a persisted Firebase session, and creates an anonymous account as a last resort. Users can also explicitly sign in via a "Sign in with Google" button that upgrades their anonymous account.

## Features

- **Google Sign-In-first** — `SignInAsync()` tries silent Google Sign-In before anything else. Returning Google users restore their session with no UI interaction. If silent sign-in fails, it falls back to a persisted Firebase session or creates an anonymous account.
- **Upgrade on demand** — `ManualSignInAsync()` shows the Google account picker. When the player signs in with Google, their existing anonymous account is **linked** (upgraded) — all game data associated with that Firebase UID is preserved.
- **Always authenticated** — `SignOutAsync()` signs out of Google and Firebase, clears state, then immediately creates a fresh anonymous account. The app never goes userless — analytics, cloud save, and leaderboards always have a Firebase user.
- `AuthenticatedInitModule` — an init module that bootstraps the session during the initializer pipeline, with timeout handling and skip/disable flags.
- `CountryService` — resolves the player country via IP lookup, caches the result, and optionally provides flag sprites via `CountryFlagDatabase` ScriptableObject.
- `CountryFlagDatabase` — editable asset mapping ISO country codes to flag sprites and localized names.

## Getting Started

### 1. Install dependencies

- [UniTask](https://github.com/Cysharp/UniTask) (`com.cysharp.unitask`)
- [Firebase Core & Auth](https://firebase.google.com/docs/unity/setup) (`com.google.firebase.app`, `com.google.firebase.auth`)
- [Google Sign-In Unity plugin](https://github.com/googlesamples/google-signin-unity) (`com.google.signin.google-signin-unity`) — needed only for the manual "Sign in with Google" button

### 2. Bootstrap (auto)

No configuration needed. At app start `AuthenticatedInitModule` calls `AuthService.SignInAsync()` which:

1. Attempts **silent Google Sign-In** — if the user previously signed in with Google, their session is restored without any UI.
2. If silent Google fails, checks Firebase for a persisted session (linked or anonymous) and restores it.
3. If no session exists, creates a new **anonymous** Firebase account.

```csharp
// State after SignInAsync() on first launch:
AuthService.IsAuthenticated      → true  (Firebase session exists)
AuthService.IsSignedIn           → false (not Google-linked yet)
AuthService.IsFirebaseAnonymous  → true
AuthService.User.UserId          → "abc123..."  (Firebase UID)

// State after SignInAsync() for a returning Google user:
AuthService.IsAuthenticated      → true
AuthService.IsSignedIn           → true  (Google-linked restored silently)
AuthService.IsFirebaseAnonymous  → false
```

### 3. Add "Sign in with Google" (manual)

Configure the Web Client ID **once** (before any UI is shown), then wire a button to `ManualSignInAsync`:

```csharp
// Do this early (e.g. before AuthenticatedInitModule runs)
AuthService.WebClientId = "YOUR_WEB_CLIENT_ID.apps.googleusercontent.com";

// Wire to a UI button
public async void OnSignInWithGoogleClicked()
{
    bool ok = await AuthService.ManualSignInAsync();
    // ok == true → account is now Google-linked
    // ok == false → user cancelled or error — still has anonymous session
}
```

The Web Client ID is created in the [Google Cloud Console](https://console.cloud.google.com/apis/credentials) under **Credentials → Create Credentials → OAuth client ID → Web application**.

### 4. Enable scripting define symbols

The assembly automatically adds:

| Symbol | Condition |
|--------|-----------|
| `BUMI_AUTH_HAS_FIREBASE` | When `com.google.firebase.app` is present |
| `BUMI_AUTH_HAS_GOOGLE_SIGNIN` | When `com.google.signin.google-signin-unity` is present |

If using custom package layouts, add the symbols manually via _Project Settings → Player → Scripting Define Symbols_.

### 5. Create the Authenticated Init Module

- Use `Create → BumiMobile → Auth → Authenticated Init Module` to create an asset.
- (Optional) Assign a `CountryFlagDatabase` for flag lookups.
- Add the module to your `ProjectInitSettings` asset so it runs before other modules.

### 6. Configure skip/disable flags (optional)

- `PlayerPrefs` key `__auth_disabled__` permanently disables auto sign-in when set to `1`.
- `PlayerPrefs` key `__reset_skip_auth__` skips the next sign-in attempt (cleared automatically).

### 7. Using Country Flags

- Create a `CountryFlagDatabase` asset and populate ISO codes with sprites and display names.
- Assign it to the init module so the service can resolve display labels and sprites.

### 8. Profile Save Integration (optional)

- Define `BUMIMOBILE_PROFILE_SAVE` to enable the helper method `CountryService.CaptureLocationOnceAsync(ProfileSave)` for saving location snapshots.

## Auth Flow

```
App Start
  └─ AuthenticatedInitModule
       ├─ Skip/disable flags? → skip auth
       └─ AuthService.SignInAsync()
            ├─ Silent Google Sign-In?
            │    ├─ Success → Firebase link/upgrade → done (Google-linked)
            │    └─ Fail/not available → continue
            ├─ Firebase has existing session? → restore it (linked OR anonymous)
            └─ No session? → SignInAnonymouslyAsync() → new anonymous account
            └─ Returns true ONLY if a linked (non-anonymous) account exists

Manual "Sign in with Google" button
  └─ AuthService.ManualSignInAsync()
       ├─ GoogleSignIn.DefaultInstance.SignIn()  // show account picker
       ├─ GoogleAuthProvider.GetCredential(idToken)  // Firebase credential
       └─ auth.CurrentUser.IsAnonymous?
            ├─ Yes → LinkWithCredentialAsync()  // upgrade anonymous → linked
            └─ No  → SignInWithCredentialAsync()  // direct sign-in
            └─ All game data tied to the Firebase UID is preserved!

Sign Out
  └─ AuthService.SignOutAsync()
       ├─ GoogleSignIn.DefaultInstance.SignOut()
       ├─ FirebaseAuth.SignOut()
       ├─ Clear PlayerId cache
       └─ SignInAnonymouslyAsync() → new anonymous account (app stays "logged in")
```

### State flags after each scenario

| Scenario | `IsAuthenticated` | `IsSignedIn` | `IsFirebaseAnonymous` |
|----------|:---:|:---:|:---:|
| First launch (after `SignInAsync`) | ✓ | ✗ | ✓ |
| Restart with linked account | ✓ | ✓ | ✗ |
| After tapping "Sign in with Google" | ✓ | ✓ | ✗ |
| After `SignOutAsync()` | ✓ | ✗ | ✓ |

## Folder Layout

- `Runtime/` — runtime scripts and assembly definition.
- `CHANGELOG.md` — version history following Keep a Changelog.
- `package.json` — Unity package manifest.

## License

See the repository root `LICENSE` file for terms.
