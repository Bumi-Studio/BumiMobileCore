# Changelog

## [0.1.1] - 2024-Current

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

## [0.1.0] - 2024-Initial

### Added

- Initial release of Bumi Mobile Gallery
- Project structure and package configuration
