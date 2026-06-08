# Bumi Mobile Build

Editor-only Android build tooling for Bumi Mobile projects.

## Features

- Shows a confirmation window before builds started from Unity's Build Player window.
- Lets the developer confirm the app version, Android bundle version code, and custom keystore credentials.
- Provides Internal Test, Cheat, and Production presets without replacing unrelated build flags or extra scripting defines.
- Stores shared configuration in `ProjectSettings/BumiMobileBuildSettings.asset`.
- Stores the last selected preset locally for each developer and project.

## Configuration

Open `Edit > Project Settings > Bumi Mobile > Build`.

- **Enable Pre-Build Window** controls whether Android builds are intercepted.
- **Internal Test Label**, **Cheat Label**, and **Production Label** control the preset names shown in the window.
- **Cheat Scripting Define** controls the define added only by the Cheat preset. Its default value is `BUMI_CHEAT_BUILD`.

The scripting define must be a valid C# preprocessor identifier. Projects migrating an existing implementation can replace the default with a project-specific define such as `SBP_CHEAT_BUILD`.

## Runtime Usage

Game code can conditionally enable cheat-only behavior:

```csharp
#if UNITY_EDITOR || BUMI_CHEAT_BUILD
    EnableCheatTools();
#endif
```

## Presets

- **Development - Internal Test** enables `BuildOptions.Development` and removes the configured cheat define.
- **Development - Cheat** enables `BuildOptions.Development` and adds the configured cheat define.
- **Production** disables `BuildOptions.Development` and removes the configured cheat define.

Builds for targets other than Android continue through Unity's default build flow without showing the confirmation window.
