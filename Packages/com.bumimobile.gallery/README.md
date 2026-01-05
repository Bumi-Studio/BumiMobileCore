# Bumi Mobile Gallery Package

A unified mobile gallery management package for iOS and Android that provides native gallery access and image upload capabilities.

## Features

- 📱 Native gallery picker for iOS and Android
- 🖼️ Image selection and preview
- 📤 Seamless image upload functionality
- 🔄 Cross-platform compatibility
- 🎯 Async/await pattern support
- ✅ Automatic permission handling
- ✅ Texture2D conversion for in-game use
- ✅ Static utility API (no setup required)

## Installation

### Prerequisites

- Unity 2021.3 or later
- iOS Xcode project (for iOS builds)
- Android SDK/NDK (for Android builds)
- `com.bumimobile.core` package installed

### Add to Package Manifest

Open `Packages/manifest.json` and add:

```json
"com.bumimobile.gallery": "0.1.5"
```

## Quick Start

### Basic Usage (Static API - No Setup Required)

```csharp
using BumiMobile.Gallery;

// Open gallery - just call the static method directly!
string imagePath = await GalleryManager.OpenGalleryAsync();

if (!string.IsNullOrEmpty(imagePath))
{
    // Show preview
    Texture2D texture = await GalleryManager.GetImageTextureAsync(imagePath);
    myImage.texture = texture;

    // Upload to server
    bool success = await GalleryManager.UploadImageAsync(
        imagePath,
        "https://your-api.com/upload"
    );
}
```

### Permission Handling

```csharp
// Check permission
if (!GalleryManager.HasGalleryPermission())
{
    // Request permission
    bool granted = await GalleryManager.RequestGalleryPermissionAsync();
}
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

**C# Scripts**:
- `GalleryManager.cs` - Main static utility class
- `IGalleryService.cs` - Interface definition
- `IOSGalleryWrapper.cs` - iOS platform layer
- `AndroidGalleryWrapper.cs` - Android platform layer
- `AndroidGalleryCallback.cs` - Android callback receiver

**Native Code**:
- iOS: `BumiGalleryManager.swift` + `BumiGalleryBridge.mm`
- Android: `GalleryManager.java` + `GalleryActivity.java`

### How It Works

The package uses native platform-specific code to open the gallery and handle callbacks:

1. **iOS**: Uses Swift's `PHPhotoLibrary` and `UIImagePickerController`
2. **Android**: Uses Android's MediaStore and Intent system
3. **Callbacks**: Results are returned via C# async/await pattern

## Dependencies

- `com.bumimobile.core` ^0.1.1

## API Reference

See [SUMMARY.md](SUMMARY.md) for complete API documentation and examples.

## Version History

See [CHANGELOG.md](CHANGELOG.md) for version changes and feature history.

For version history and changes, see [CHANGELOG.md](CHANGELOG.md).

## License

MIT License - See [LICENSE](LICENSE) file for details.
