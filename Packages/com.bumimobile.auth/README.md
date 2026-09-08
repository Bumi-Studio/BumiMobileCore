# Bumi Mobile Auth

Google Sign-In-first authentication for Bumi Mobile projects. This package attempts silent Google Sign-In at app start (restoring returning users seamlessly), falls back to restoring a persisted Firebase session, and creates an anonymous account as a last resort. Users can also explicitly sign in via a "Sign in with Google" button that upgrades their anonymous account.

## Features

- **Google Sign-In-first** — `SignInAsync()` tries silent Google Sign-In before anything else. Returning Google users restore their session with no UI interaction. If silent sign-in fails, it falls back to a persisted Firebase session or creates an anonymous account.
- **Upgrade on demand** — `ManualSignInAsync()` (Google) and `ManualSignInWithAppleAsync()` (Apple) show the provider account flow. When the player signs in, their existing anonymous account is **linked** (upgraded) — all game data associated with that Firebase UID is preserved.
- **Sign in with Apple** — `ManualSignInWithAppleAsync()` runs the native Apple ID flow on iOS/macOS and links/upgrades Firebase the same way as Google. `IsAppleSignInSupported` reports whether the platform can offer it.
- **Always authenticated** — `SignOutAsync()` signs out of Google and Firebase, clears state, then immediately creates a fresh anonymous account. The app never goes userless — analytics, cloud save, and leaderboards always have a Firebase user.
- `AuthenticatedInitModule` — an init module that bootstraps the session during the initializer pipeline, with timeout handling and skip/disable flags.
- `CountryService` — resolves the player country via IP lookup, caches the result, and optionally provides flag sprites via `CountryFlagDatabase` ScriptableObject.
- `CountryFlagDatabase` — editable asset mapping ISO country codes to flag sprites and localized names.

## Getting Started

### 1. Install dependencies

- [UniTask](https://github.com/Cysharp/UniTask) (`com.cysharp.unitask`)
- [Firebase Core & Auth](https://firebase.google.com/docs/unity/setup) (`com.google.firebase.app`, `com.google.firebase.auth`)
- [Google Sign-In Unity plugin](https://github.com/googlesamples/google-signin-unity) (`com.google.signin.google-signin-unity`) — needed only for the manual "Sign in with Google" button
- [Sign in with Apple Unity plugin](https://github.com/lupidan/apple-signin-unity) (`com.lupidan.apple-signin-unity`) — needed only for the manual "Sign in with Apple" button

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
AuthService.Initialize("YOUR_WEB_CLIENT_ID.apps.googleusercontent.com");

// Wire to a UI button
public async void OnSignInWithGoogleClicked()
{
    bool ok = await AuthService.ManualSignInAsync();
    // ok == true → account is now Google-linked
    // ok == false → user cancelled or error — still has anonymous session
}
```

Use `AuthService.OnSignInCompleted` for systems that need to react to the completed authentication operation. `OnFirebaseAuthChanged` observes the Firebase state transition and may be raised before the sign-in method returns, so it should not be used as the UI completion signal.

The Web Client ID is created in the [Google Cloud Console](https://console.cloud.google.com/apis/credentials) under **Credentials → Create Credentials → OAuth client ID → Web application**.

#### iOS Setup (additional)

Google Sign-In on iOS requires additional native integration beyond Android:

1. **Reversed Client ID URL scheme** — Add the following to your `Info.plist` (replacing `{CLIENT_ID}` with your OAuth client ID without the `.apps.googleusercontent.com` suffix):
   ```xml
   <key>CFBundleURLTypes</key>
   <array>
     <dict>
       <key>CFBundleURLSchemes</key>
       <array>
         <string>com.googleusercontent.apps.{CLIENT_ID}</string>
       </array>
     </dict>
   </array>
   ```
2. **Google Sign-In iOS SDK** — The Google Sign-In Unity plugin (`com.google.signin`) typically includes the iOS native library. Ensure the SDK is present (CocoaPods or embedded `.framework`).
3. **`openURL` hook** — The plugin must receive the OAuth callback. Most Google Sign-In Unity plugins handle this via `UnityAppController` swizzling. If using a custom `UnityAppController` subclass, ensure `openURL:` is forwarded to `GIDSignIn`.
4. **Firebase iOS setup** — Add `GoogleService-Info.plist` to your Xcode project (with `REVERSED_CLIENT_ID` matching the URL scheme above).

For detailed iOS steps, refer to the [Google Sign-In Unity plugin docs](https://github.com/googlesamples/google-signin-unity).

### 3b. Add "Sign in with Apple" (manual)

Install `com.lupidan.apple-signin-unity`, then wire a button to `ManualSignInWithAppleAsync`:

```csharp
// Show/hide the button based on platform support
appleButton.gameObject.SetActive(AuthService.IsAppleSignInSupported);

public async void OnSignInWithAppleClicked()
{
    bool ok = await AuthService.ManualSignInWithAppleAsync();
    // ok == true  → account is now Apple-linked
    // ok == false → user cancelled or error — still has anonymous session
}
```

No `WebClientId` is needed for Apple. The service generates the nonce, drives the
native flow, and exchanges the Apple identity token for a Firebase credential
(`apple.com` OAuth provider). `OnSignInCompleted` fires with the result, same as the
Google path.

**iOS/Xcode:** enable the **Sign in with Apple** capability and entitlement on the
Unity target. In Bumi Mobile projects the app's build post-processor adds this
automatically; otherwise use `AppleAuth.Editor.ProjectCapabilityManager` or add it by
hand in Xcode. Enable the provider in the Firebase console (**Authentication →
Sign-in method → Apple**).

### 4. Enable scripting define symbols

The assembly automatically adds:

| Symbol | Condition |
|--------|-----------|
| `BUMI_AUTH_HAS_FIREBASE` | When `com.google.firebase.app` is present |
| `BUMI_AUTH_HAS_GOOGLE_SIGNIN` | When `com.google.signin.google-signin-unity` is present |
| `BUMI_AUTH_HAS_APPLE_SIGNIN` | When `com.lupidan.apple-signin-unity` is present |

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

Manual "Sign in with Google" button          Manual "Sign in with Apple" button
  └─ AuthService.ManualSignInAsync()            └─ AuthService.ManualSignInWithAppleAsync()
       ├─ Google account flow                        ├─ AppleAuthManager.LoginWithAppleId()  // native, hashed nonce
       ├─ GoogleAuthProvider.GetCredential()         ├─ OAuthProvider.GetCredential("apple.com", idToken, rawNonce)
       └─ TryAuthFirebaseWithCredentialAsync() ──┬── └─ TryAuthFirebaseWithCredentialAsync()
                                                 │
                          shared Firebase step:  ├─ auth.CurrentUser.IsAnonymous?
                                                 │    ├─ Yes → LinkWithCredentialAsync()   // upgrade anonymous → linked
                                                 │    │        └─ already in use? → SignInWithCredentialAsync() into that account
                                                 │    └─ No  → SignInWithCredentialAsync() // direct sign-in
                                                 └─ All game data tied to the Firebase UID is preserved!

Sign Out
  └─ AuthService.SignOutAsync()
       ├─ GoogleSignIn.DefaultInstance.SignOut()
       ├─ FirebaseAuth.SignOut()
       ├─ Clear PlayerId cache
       └─ SignInAnonymouslyAsync() → new anonymous account (app stays "logged in")
```

### State flags after each scenario

| Scenario | `IsAuthenticated` | `IsSignedIn` | `IsFirebaseAnonymous` | `User` |
|----------|:---:|:---:|:---:|:---|
| First launch (after `SignInAsync`) | ✓ | ✗ | ✓ | Anonymous |
| Restart with linked account | ✓ | ✓ | ✗ | Linked |
| After tapping "Sign in with Google" | ✓ | ✓ | ✗ | Linked |
| After tapping "Sign in with Apple" | ✓ | ✓ | ✗ | Linked |
| After `SignOutAsync()` | ✓ | ✗ | ✓ | **Anonymous** (not `null`!) |

## Folder Layout

- `Runtime/` — runtime scripts and assembly definition.
- `CHANGELOG.md` — version history following Keep a Changelog.
- `package.json` — Unity package manifest.

## License

See the repository root `LICENSE` file for terms.
