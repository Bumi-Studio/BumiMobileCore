# Bumi Mobile Build

Editor-only Android build tooling for Unity 6 projects.

## Requirements

- Unity 6000.0 or newer.
- At least one Android Build Profile.
- **Customize Player Settings** enabled on every profile whose version and signing settings should be editable from the confirmation window.

## Setup

1. Open `File > Build Profiles`.
2. Create the required Android profiles, such as Development and Production.
3. Configure **Development Build** and **Build App Bundle** on each profile.
4. Enable **Customize Player Settings** on each profile.
5. Configure the initial app version and bundle version code on each profile.
6. Configure Android publishing settings on each profile.

The selected Build Profile is the source of truth for build type, APK/AAB format, app version, bundle version code, Android signing settings, Player Settings, and scripting defines.

## Example Build Profiles

A typical Android project should keep separate profiles for development and release builds:

| Profile | Development Build | Package | Version example | Bundle code example | Signing |
| --- | --- | --- | --- | --- | --- |
| `Development - Cheat` | Enabled | APK | `1.7.00-dev` | `24` | Debug or development keystore |
| `Internal Test` | Disabled | AAB | `1.7.00-rc1` | `24` | Upload keystore |
| `Production` | Disabled | AAB | `1.7.00` | `24` | Upload keystore |

Each profile stores its own editable values in the Build Profile asset:

```yaml
bundleVersion: 1.7.00
AndroidBundleVersionCode: 24
androidUseCustomKeystore: 1
AndroidKeystoreName: '{inproject}: Keystore/upload.keystore'
AndroidKeyaliasName: upload
```

Changing `Production` in the confirmation window updates only `Production.asset`; it should not overwrite the version or signing fields in `Development - Cheat.asset` or `Internal Test.asset`.

## Confirmation Window

Android builds started from Unity's Build Profiles or Build Player UI show `Confirm Build Profile` before the build runs.

The window provides:

- Selection from all Android Build Profiles in the project.
- Read-only Development/Production, APK/AAB, and cheat status derived from the selected profile.
- App version and Android bundle version code editing.
- Custom keystore path, alias, and password editing.
- Validation for version code and custom keystore credentials.

Switching profiles in the window loads that profile's settings without immediately activating it. If Unity must switch the active target to Android before building, the build resumes automatically after the switch completes.

Version, Android bundle version code, keystore enablement, keystore path, and key alias are saved to the selected Build Profile asset. Keystore passwords are stored per profile in the current developer's `EditorPrefs`, applied only during the scheduled build, and cleared afterward.

Development profiles use Unity's built-in `DEVELOPMENT_BUILD` scripting define. Projects can use it directly:

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    EnableDevelopmentTools();
#endif
```

## Project Settings

Open `Edit > Project Settings > Bumi Mobile > Build`.

**Enable Pre-Build Window** controls whether Android builds are intercepted. Builds for other targets always continue through Unity's default build handler.
