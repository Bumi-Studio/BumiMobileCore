# Bumi Mobile Auth

Anonymous-first authentication for Bumi Mobile projects. This package auto-initialises a Firebase session at app start (restoring any persisted account or creating an anonymous one), then lets the user upgrade to a permanent Google-linked account via a "Sign in with Google" button.

## Features

- **Anonymous-first** — `SignInAsync()` skips Google entirely. On first launch the player gets an anonymous Firebase UID immediately (works with Firestore, RTDB, etc.). If a linked account already exists, Firebase restores it transparently — no Google sign-in needed.
- **Upgrade on demand** — `ManualSignInAsync()` shows the Google account picker. When the player signs in with Google, their existing anonymous account is **linked** (upgraded) — all game data associated with that Firebase UID is preserved.
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

1. Checks Firebase — if a previous session exists (linked or anonymous), restores it.
2. If no session exists, creates a new **anonymous** Firebase account.

```csharp
// State after SignInAsync():
AuthService.IsAuthenticated      → true  (Firebase session exists)
AuthService.IsSignedIn           → false (not Google-linked yet)
AuthService.IsFirebaseAnonymous  → true
AuthService.User.UserId          → "abc123..."  (Firebase UID)
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
            └─ EnsureFirebaseAnonIfPossibleAsync()
                 ├─ Firebase has existing session? → restore it (linked OR anonymous)
                 └─ No session? → SignInAnonymouslyAsync() → new anonymous account
            └─ Returns true ONLY if a linked (non-anonymous) account was restored

Manual "Sign in with Google" button
  └─ AuthService.ManualSignInAsync()
       ├─ EnsureFirebaseAnonIfPossibleAsync()  // guarantee anonymous session exists
       ├─ GoogleSignIn.DefaultInstance.SignIn()  // show account picker
       ├─ GoogleAuthProvider.GetCredential(idToken)  // Firebase credential
       └─ auth.CurrentUser.LinkWithCredentialAsync()  // upgrade anonymous → linked
            └─ All game data tied to the Firebase UID is preserved!
```

### State flags after each scenario

| Scenario | `IsAuthenticated` | `IsSignedIn` | `IsFirebaseAnonymous` |
|----------|:---:|:---:|:---:|
| First launch (after `SignInAsync`) | ✓ | ✗ | ✓ |
| Restart with linked account | ✓ | ✓ | ✗ |
| After tapping "Sign in with Google" | ✓ | ✓ | ✗ |
| After `SignOutAsync()` + restart | ✓ | ✗ | ✓ |

## Folder Layout

- `Runtime/` — runtime scripts and assembly definition.
- `CHANGELOG.md` — version history following Keep a Changelog.
- `package.json` — Unity package manifest.

## License

See the repository root `LICENSE` file for terms.
