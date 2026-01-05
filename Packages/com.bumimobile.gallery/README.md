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

## Installation

### Prerequisites

- Unity 2021.3 or later
- iOS Xcode project (for iOS builds)
- Android SDK/NDK (for Android builds)
- `com.bumimobile.core` package installed

### Add to Package Manifest

Open `Packages/manifest.json` and add:

```json
"com.bumimobile.gallery": "0.1.0"
```

## Quick Start

### Basic Usage

```csharp
using BumiMobile.Gallery;

// Get the singleton instance
var gallery = GalleryManager.Instance;

// Open gallery
string imagePath = await gallery.OpenGalleryAsync();

if (!string.IsNullOrEmpty(imagePath))
{
    // Show preview
    Texture2D texture = await gallery.GetImageTextureAsync(imagePath);

    // Upload to server
    bool success = await gallery.UploadImageAsync(
        imagePath,
        "https://your-api.com/upload"
    );
}
```

### Permission Handling

```csharp
// Check permission
if (!GalleryManager.Instance.HasGalleryPermission())
{
    // Request permission
    bool granted = await GalleryManager.Instance.RequestGalleryPermissionAsync();
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

## Package Structure

```
com.bumimobile.gallery/
├── Runtime/
│   ├── Scripts/
│   │   ├── GalleryManager.cs          # Main singleton manager
│   │   ├── IGalleryService.cs         # Service interface
│   │   ├── IOSGalleryWrapper.cs       # iOS implementation
│   │   ├── AndroidGalleryWrapper.cs   # Android implementation
│   │   └── Examples/
│   │       └── GalleryExample.cs      # Example usage
│   └── Plugins/
│       ├── iOS/
│       │   ├── BumiGalleryManager.swift    # Swift implementation
│       │   └── BumiGalleryBridge.h         # C# ↔ Swift bridge
│       └── Android/
│           ├── GalleryManager.java         # Java implementation
│           └── AndroidManifest.xml         # Permissions config
└── Editor/                                  # Editor utilities
```

## Dependencies

- `com.bumimobile.core` ^0.1.1

## Documentation

For detailed API reference and usage examples, see [SUMMARY.md](SUMMARY.md).

For version history and changes, see [CHANGELOG.md](CHANGELOG.md).

## License

MIT License - See [LICENSE](LICENSE) file for details.
