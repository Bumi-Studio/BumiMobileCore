# Changelog

All notable changes to the Bumi Mobile Gallery package.

## [0.1.3] - 2026-01-05

### Package Structure

**Core Files:**

- `package.json` - Package manifest with dependencies on com.bumimobile.core v0.1.2
- `README.md` - Quick start guide and feature overview
- `SUMMARY.md` - Complete API reference and integration patterns
- `CHANGELOG.md` - Version history

**Plugins/NativeGallery/:**

- `NativeGallery.cs` (988 lines) - Main static API with comprehensive gallery operations
  - Image/Video selection (single and multiple)
  - Media properties extraction (dimensions, orientation, duration)
  - Permission handling (Read/Write)
  - Save to gallery functionality
  - LoadImageAtPath with max size and orientation support
  - Video thumbnail generation

**iOS Implementation (Plugins/NativeGallery/iOS/):**

- `NativeGallery.mm` - Objective-C++ bridge for Swift/iOS integration
- `NGMediaReceiveCallbackiOS.cs` - Callback handler for media selection
- `NGMediaSaveCallbackiOS.cs` - Callback handler for save operations
- `NGPermissionCallbackiOS.cs` - Permission request callback handler

**Android Implementation (Plugins/NativeGallery/Android/):**

- `NativeGallery.aar` - Android Archive Library with Java implementation
- `NGCallbackHelper.cs` - Unity callback routing helper
- `NGMediaReceiveCallbackAndroid.cs` - Media selection callback handler
- `NGPermissionCallbackAndroid.cs` - Permission callback handler

**Editor Tools (Plugins/NativeGallery/Editor/):**

- `NGPostProcessBuild.cs` - Automatic build post-processing for iOS/Android
- `BumiMobile.Gallery.Editor.asmdef` - Editor assembly definition

**Assembly Definitions:**

- `BumiMobile.Gallery.asmdef` - Main runtime assembly

### API Features

**Media Selection:**

- `GetImageFromGallery()` - Single image picker with async callback
- `GetImagesFromGallery()` - Multiple image picker
- `GetVideoFromGallery()` - Single video picker with async callback
- `GetVideosFromGallery()` - Multiple video picker
- `GetAudioFromGallery()` - Audio file picker
- `GetMixedMediaFromGallery()` - Mixed media type picker

**Media Properties:**

- `GetImageProperties()` - Extract image dimensions, MIME type, orientation
- `GetVideoProperties()` - Extract video dimensions, duration, rotation

**Permission Management:**

- `CheckPermission()` - Check Read/Write permission status
- `RequestPermission()` - Request gallery access permission
- `OpenSettings()` - Open app settings for manual permission grant

**Image Operations:**

- `LoadImageAtPath()` - Load image from path with max size constraint
- `GetImageOrientation()` - Get image orientation (EXIF data)
- `SaveImageToGallery()` - Save Texture2D/byte[] to gallery with album support

**Video Operations:**

- `SaveVideoToGallery()` - Save video to gallery
- `GetVideoThumbnail()` - Generate video thumbnail with time and max size

**Platform Support:**

- iOS 12.0+ with UIImagePickerController and PHPhotoLibrary
- Android API 21+ (5.0 Lollipop) with MediaStore and Storage Access Framework
- Editor preview mode for development

### Dependencies

- Unity 2021.3 or later
- com.bumimobile.core v0.1.2

### Technical Architecture

**Cross-Platform Design:**

- Static utility class (no MonoBehaviour required)
- Conditional compilation (#if UNITY_IOS/UNITY_ANDROID/UNITY_EDITOR)
- Native callback routing through UnitySendMessage
- Automatic permission handling

**iOS Implementation:**

- Native UIImagePickerController for media selection
- PHPhotoLibrary for permission and gallery access
- Objective-C++ bridge (NativeGallery.mm) for C#-Swift communication
- Callback system using MonoBehaviour receivers

**Android Implementation:**

- MediaStore API for gallery access
- Storage Access Framework (SAF) for Android 10+
- ActivityCompat for runtime permissions
- Custom Activity result handling via .aar library

**Editor Mode:**

- File dialog fallback for testing in Unity Editor
- Simulated permission system
- Image loading support for development workflow

## [0.1.1] - 2026-01-05

### Added

- Complete C# ↔ Native bridge architecture for iOS and Android
- Static utility API (no singleton, no setup required)
- Full async/await support with proper TaskCompletionSource handling
- Comprehensive error handling and validation
- Detailed API documentation and usage examples

### Changed

- **BREAKING**: Converted `GalleryManager` from MonoBehaviour singleton to static utility class
  - Before: `await GalleryManager.Instance.OpenGalleryAsync()`
  - After: `await GalleryManager.OpenGalleryAsync()`
- Refactored iOS C bridge (`BumiGalleryBridge.mm`) for proper Swift callback handling
- Enhanced Android Activity lifecycle integration (`GalleryActivity.java`)
- Improved callback routing through native UnitySendMessage system

### Fixed

- iOS callbacks now properly routed through C bridge to C# code
- Android Activity.onActivityResult() now properly intercepted
- Permission request results now correctly handled on both platforms
- Image texture loading now handles various formats properly
- Upload multipart form data now properly encoded
- Removed duplicate callback handling on Android

### Documentation

- Updated all examples to use static API
- Added complete integration patterns
- Added architecture documentation
- Consolidated all docs into 3 files (README, SUMMARY, CHANGELOG)
- Removed redundant documentation files

### Technical Details

**iOS Implementation:**

- Native UIImagePickerController with PHPhotoLibrary permissions
- Objective-C++ bridge (BumiGalleryBridge.mm) for Swift-C# communication
- Proper callback routing through UnitySendMessage

**Android Implementation:**

- Custom GalleryActivity extending UnityPlayerActivity
- MediaStore intents for gallery selection
- Runtime permission handling via ActivityCompat
- Callback routing through AndroidGalleryCallback MonoBehaviour

**C# Architecture:**

- Static GalleryManager class with all public methods
- TaskCompletionSource<T> for proper async/await handling
- Platform abstraction via #if UNITY_IOS/UNITY_ANDROID
- IOSGalleryWrapper and AndroidGalleryWrapper for platform-specific logic

## [0.1.0] - 2026-01-05

### Added

- Initial release of Bumi Mobile Gallery
- Project structure and package configuration
