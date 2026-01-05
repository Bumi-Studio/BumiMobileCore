# Bumi Mobile Gallery Package

A comprehensive mobile gallery management package for iOS and Android featuring the powerful NativeGallery plugin by yasirkula.

## Features

- 📱 Native gallery picker for iOS and Android
- 🖼️ Image, video, and audio selection (single or multiple)
- 💾 Save images/videos to device gallery
- 📐 Image properties (dimensions, orientation, MIME type)
- 🎬 Video properties (duration, rotation, dimensions)
- 🖼️ Video thumbnail generation
- 🔄 Cross-platform compatibility (iOS 12+, Android 5.0+)
- 🎯 Async callback pattern support
- ✅ Automatic permission handling (Read/Write)
- ✅ Texture2D conversion with max size and orientation handling
- ✅ Static utility API (no MonoBehaviour required)
- 🔧 Editor mode support for testing

## Installation

### Prerequisites

- Unity 2021.3 or later
- iOS Xcode project (for iOS builds)
- Android SDK/NDK (for Android builds)
- `com.bumimobile.core` package installed

### Add to Package Manifest

Open `Packages/manifest.json` and add:

```json
"com.bumimobile.gallery": "0.1.2"
```

## Quick Start

### Basic Image Selection

```csharp
// Pick single image with callback
NativeGallery.GetImageFromGallery((path) =>
{
    if (path != null)
    {
        // Load texture with max size
        Texture2D texture = NativeGallery.LoadImageAtPath(path, maxSize: 2048);
        if (texture != null)
        {
            myRawImage.texture = texture;
        }
    }
}, "Select Image", "image/*");
```

### Multiple Image Selection

```csharp
// Pick multiple images
NativeGallery.GetImagesFromGallery((paths) =>
{
    if (paths != null)
    {
        Debug.Log($"Selected {paths.Length} images");
        foreach (string path in paths)
        {
            // Process each image
        }
    }
}, "Select Images", "image/*");
```

### Permission Handling

```csharp
// Check permission
NativeGallery.Permission permission = NativeGallery.CheckPermission(
    NativeGallery.PermissionType.Read,
    NativeGallery.MediaType.Image
);

if (permission != NativeGallery.Permission.Granted)
{
    // Request permission
    permission = NativeGallery.RequestPermission(
        NativeGallery.PermissionType.Read,
        NativeGallery.MediaType.Image
    );
}
```

### Save Image to Gallery

```csharp
// Save texture to gallery
NativeGallery.SaveImageToGallery(texture, "MyApp", "screenshot.png", (success, path) =>
{
    Debug.Log(success ? $"Saved to {path}" : "Save failed");
});
```

## Platform Setup

### iOS

Add to your `Info.plist`:

```xml
<key>NSPhotoLibraryUsageDescription</key>
<string>We need access to your photo library to upload images.</string>

<key>NSPhotoLibraryAddOnlyUsageDescription</key>
<string>We need permission to save images to your photo library.</string>
```

Ensure these frameworks are linked in Xcode:

- `Photos.framework`
- `UIKit.framework`
- `Foundation.framework`

### Android

Required permissions (auto-merged from package):

- `android.permission.READ_EXTERNAL_STORAGE`
- `android.permission.READ_MEDIA_IMAGES` (Android 13+)
- `android.permission.INTERNET`

Ensure your `build.gradle` includes:

```gradle
android {
    compileSdkVersion 33

    defaultConfig {
        minSdkVersion 21
        targetSdkVersion 33
    }
}
```

## Supported Platforms

- iOS 12.0+
- Android 5.0+ (API level 21+)

## Architecture

### What's Included

**Core API**:

- `NativeGallery.cs` (988 lines) - Main static utility class with comprehensive gallery operations

**iOS Implementation** (`Plugins/NativeGallery/iOS/`):

- `NativeGallery.mm` - Objective-C++ bridge for Unity-iOS communication
- `NGMediaReceiveCallbackiOS.cs` - Media selection callback handler
- `NGMediaSaveCallbackiOS.cs` - Save operation callback handler
- `NGPermissionCallbackiOS.cs` - Permission request callback handler

**Android Implementation** (`Plugins/NativeGallery/Android/`):

- `NativeGallery.aar` - Android Archive Library with native Java implementation
- `NGCallbackHelper.cs` - Unity callback routing helper
- `NGMediaReceiveCallbackAndroid.cs` - Media selection callback handler
- `NGPermissionCallbackAndroid.cs` - Permission callback handler

**Editor Tools** (`Plugins/NativeGallery/Editor/`):

- `NGPostProcessBuild.cs` - Automatic build configuration for iOS/Android
- `BumiMobile.Gallery.Editor.asmdef` - Editor assembly definition

### How It Works

The package uses native platform-specific implementations:

1. **iOS**: Uses `UIImagePickerController` and `PHPhotoLibrary` for gallery access
2. **Android**: Uses MediaStore API and Storage Access Framework (Android 10+)
3. **Callbacks**: Results returned via MonoBehaviour receivers with UnitySendMessage
4. **Permissions**: Automatic runtime permission handling on both platforms

## Dependencies

- `com.bumimobile.core` ^0.1.2

## API Reference

See [SUMMARY.md](SUMMARY.md) for complete API documentation and examples.

## Version History

See [CHANGELOG.md](CHANGELOG.md) for version changes and feature history.

For version history and changes, see [CHANGELOG.md](CHANGELOG.md).

## License

MIT License - See [LICENSE](LICENSE) file for details.
