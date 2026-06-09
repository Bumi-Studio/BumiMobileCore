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
5. Configure the initial app version, bundle version code, and Android publishing settings.

The selected Build Profile is the source of truth for build type, APK/AAB format, Player Settings, and scripting defines.

## Confirmation Window

Android builds started from Unity's Build Profiles or Build Player UI show `Confirm Build Profile` before the build runs.

The window provides:

- Selection from all Android Build Profiles in the project.
- Read-only Development/Production, APK/AAB, and cheat status derived from the active profile.
- App version and Android bundle version code editing.
- Custom keystore path, alias, and password editing.
- Validation for version code and custom keystore credentials.

Switching profiles activates the selected profile and reopens the window after Unity finishes any required domain reload. Unsaved edits must be discarded before switching.

Keystore passwords are stored per profile in the current developer's `EditorPrefs`. Version and publishing settings are saved to the selected Build Profile asset.

Development profiles use Unity's built-in `DEVELOPMENT_BUILD` scripting define. Projects can use it directly:

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    EnableDevelopmentTools();
#endif
```

## Project Settings

Open `Edit > Project Settings > Bumi Mobile > Build`.

**Enable Pre-Build Window** controls whether Android builds are intercepted. Builds for other targets always continue through Unity's default build handler.
