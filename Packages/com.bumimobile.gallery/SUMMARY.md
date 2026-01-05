# Bumi Mobile Gallery - Complete API Reference

## Quick Overview

**GalleryManager** is a static utility class providing cross-platform gallery access. No setup required - just call methods directly!

```csharp
// Open gallery
var imagePath = await GalleryManager.OpenGalleryAsync();

// Load texture
var texture = await GalleryManager.GetImageTextureAsync(imagePath);

// Upload image
bool success = await GalleryManager.UploadImageAsync(imagePath, url, "photo");

// Check/request permissions
if (!GalleryManager.HasGalleryPermission())
    await GalleryManager.RequestGalleryPermissionAsync();
```

---

## Table of Contents

1. [API Reference](#api-reference)
2. [Usage Examples](#usage-examples)
3. [Integration Patterns](#integration-patterns)
4. [Setup Details](#setup-details)
5. [Troubleshooting](#troubleshooting)

---

## API Reference

### Namespace
`BumiMobile.Gallery`

### GalleryManager

Static utility class for gallery operations across iOS and Android.

#### OpenGalleryAsync()
Opens the native gallery picker.

**Returns:** `Task<string>` - Path to selected image, or null if cancelled

**Example:**
```csharp
var imagePath = await GalleryManager.OpenGalleryAsync();
if (!string.IsNullOrEmpty(imagePath))
    Debug.Log($"Selected: {imagePath}");
```

#### GetImageTextureAsync(string imagePath)
Loads an image file as Texture2D for in-game display.

**Parameters:**
- `imagePath` (string) - Local path to image file

**Returns:** `Task<Texture2D>` - Loaded texture or null

**Example:**
```csharp
var texture = await GalleryManager.GetImageTextureAsync(imagePath);
if (texture != null)
    myImage.texture = texture;
```

#### UploadImageAsync(string imagePath, string uploadUrl, string fieldName = "image")
Uploads an image to server using multipart form data.

**Parameters:**
- `imagePath` (string) - Local path to image
- `uploadUrl` (string) - Server endpoint URL
- `fieldName` (string, optional) - Form field name (default: "image")

**Returns:** `Task<bool>` - True if successful

**Example:**
```csharp
bool success = await GalleryManager.UploadImageAsync(
    imagePath,
    "https://api.example.com/upload",
    "profile_photo"
);
```

#### HasGalleryPermission()
Checks if gallery permission is granted.

**Returns:** `bool` - True if permitted

**Example:**
```csharp
if (GalleryManager.HasGalleryPermission())
    await OpenGallery();
```

#### RequestGalleryPermissionAsync()
Requests gallery access permission from user.

**Returns:** `Task<bool>` - True if granted

**Example:**
```csharp
bool granted = await GalleryManager.RequestGalleryPermissionAsync();
```

### IGalleryService Interface

Interface for dependency injection or custom implementations:

```csharp
public interface IGalleryService
{
    Task<string> OpenGalleryAsync();
    Task<bool> UploadImageAsync(string imagePath, string uploadUrl, string fieldName = "image");
    Task<Texture2D> GetImageTextureAsync(string imagePath);
    bool HasGalleryPermission();
    Task<bool> RequestGalleryPermissionAsync();
}
```

---

## Usage Examples

### Example 1: Simple Image Selection
```csharp
using BumiMobile.Gallery;

async void SelectImage()
{
    var imagePath = await GalleryManager.OpenGalleryAsync();
    if (!string.IsNullOrEmpty(imagePath))
        Debug.Log($"Selected: {imagePath}");
}
```

### Example 2: Select and Preview
```csharp
async void SelectWithPreview()
{
    var imagePath = await GalleryManager.OpenGalleryAsync();
    if (!string.IsNullOrEmpty(imagePath))
    {
        var texture = await GalleryManager.GetImageTextureAsync(imagePath);
        previewImage.texture = texture;
    }
}
```

### Example 3: Full Workflow
```csharp
async void SelectPreviewAndUpload()
{
    // Step 1: Check/request permission
    if (!GalleryManager.HasGalleryPermission())
    {
        if (!await GalleryManager.RequestGalleryPermissionAsync())
            return;
    }

    // Step 2: Open gallery
    var imagePath = await GalleryManager.OpenGalleryAsync();
    if (string.IsNullOrEmpty(imagePath)) return;

    // Step 3: Show preview
    var texture = await GalleryManager.GetImageTextureAsync(imagePath);
    if (texture != null)
        previewImage.texture = texture;

    // Step 4: Upload
    bool success = await GalleryManager.UploadImageAsync(
        imagePath,
        "https://api.example.com/upload",
        "image"
    );
    
    Debug.Log(success ? "Upload successful!" : "Upload failed");
}
```

### Example 4: Error Handling
```csharp
async void SafeGalleryOperation()
{
    try
    {
        if (!GalleryManager.HasGalleryPermission())
        {
            bool granted = await GalleryManager.RequestGalleryPermissionAsync();
            if (!granted)
            {
                Debug.LogError("Permission required");
                return;
            }
        }

        var imagePath = await GalleryManager.OpenGalleryAsync();
        if (string.IsNullOrEmpty(imagePath))
        {
            Debug.Log("User cancelled");
            return;
        }

        if (!System.IO.File.Exists(imagePath))
        {
            Debug.LogError("File not found");
            return;
        }

        var texture = await GalleryManager.GetImageTextureAsync(imagePath);
        if (texture == null)
        {
            Debug.LogError("Failed to load texture");
            return;
        }

        bool success = await GalleryManager.UploadImageAsync(
            imagePath,
            "https://api.example.com/upload"
        );
        
        if (success)
            Debug.Log("Success!");
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"Error: {ex.Message}");
    }
}
```

---

## Integration Patterns

### Pattern 1: UI Button Integration
```csharp
using UnityEngine;
using UnityEngine.UI;
using BumiMobile.Gallery;

public class GalleryUI : MonoBehaviour
{
    [SerializeField] private Button selectButton;
    [SerializeField] private RawImage preview;
    [SerializeField] private Text statusText;

    void Start()
    {
        selectButton.onClick.AddListener(OnSelectClicked);
    }

    async void OnSelectClicked()
    {
        statusText.text = "Opening gallery...";
        var path = await GalleryManager.OpenGalleryAsync();

        if (!string.IsNullOrEmpty(path))
        {
            statusText.text = "Loading texture...";
            var texture = await GalleryManager.GetImageTextureAsync(path);
            preview.texture = texture;
            statusText.text = "Ready to upload";
        }
        else
        {
            statusText.text = "Cancelled";
        }
    }
}
```

### Pattern 2: Profile Photo Upload
```csharp
using UnityEngine;
using BumiMobile.Gallery;

public class ProfileManager : MonoBehaviour
{
    private string profilePhotoPath;

    public async void UploadProfilePhoto()
    {
        // Step 1: Select
        profilePhotoPath = await GalleryManager.OpenGalleryAsync();
        if (string.IsNullOrEmpty(profilePhotoPath))
            return;

        // Step 2: Preview
        var texture = await GalleryManager.GetImageTextureAsync(profilePhotoPath);
        if (texture != null)
            DisplayPreview(texture);

        // Step 3: Upload
        bool success = await GalleryManager.UploadImageAsync(
            profilePhotoPath,
            "https://api.myapp.com/profile/photo",
            "profile_photo"
        );

        if (success)
            SaveProfileLocally();
    }

    void DisplayPreview(Texture2D texture) { /* ... */ }
    void SaveProfileLocally() { /* ... */ }
}
```

### Pattern 3: Reactive Gallery Manager
```csharp
using UnityEngine;
using BumiMobile.Gallery;
using System;

public class ReactiveGallery : MonoBehaviour
{
    public event Action<string> OnImageSelected;
    public event Action<Texture2D> OnImageLoaded;
    public event Action<bool> OnUploadComplete;

    public async void SelectAndNotify()
    {
        var imagePath = await GalleryManager.OpenGalleryAsync();
        if (!string.IsNullOrEmpty(imagePath))
        {
            OnImageSelected?.Invoke(imagePath);

            var texture = await GalleryManager.GetImageTextureAsync(imagePath);
            if (texture != null)
                OnImageLoaded?.Invoke(texture);
        }
    }

    public async void UploadAndNotify(string imagePath, string url)
    {
        bool success = await GalleryManager.UploadImageAsync(imagePath, url);
        OnUploadComplete?.Invoke(success);
    }
}
```

---

## Setup Details

### iOS Setup

**Info.plist Requirements:**
```xml
<key>NSPhotoLibraryUsageDescription</key>
<string>We need access to your photos</string>

<key>NSPhotoLibraryAddOnlyUsageDescription</key>
<string>We need permission to save images</string>
```

**Xcode Framework Linking:**
- Photos.framework
- UIKit.framework
- Foundation.framework

**Build Settings:**
- Minimum iOS version: 12.0
- Swift Language Version: 5.0+

### Android Setup

**AndroidManifest.xml:**
Already includes required permissions:
- `android.permission.READ_EXTERNAL_STORAGE`
- `android.permission.READ_MEDIA_IMAGES`
- `android.permission.INTERNET`

**Gradle Configuration:**
```gradle
android {
    compileSdkVersion 33
    
    defaultConfig {
        minSdkVersion 21
        targetSdkVersion 33
    }
}
```

### Runtime Initialization

**No setup required!** GalleryManager is static and ready to use immediately.

---

## Troubleshooting

### Gallery Won't Open

**Cause**: Missing permissions or not granted by user

**Solution**:
```csharp
if (!GalleryManager.HasGalleryPermission())
{
    await GalleryManager.RequestGalleryPermissionAsync();
}
var imagePath = await GalleryManager.OpenGalleryAsync();
```

**Also check:**
- iOS: Verify `NSPhotoLibraryUsageDescription` in Info.plist
- Android: Check device Settings → Apps → Permissions → Photos

### Upload Fails

**Cause**: Invalid URL or network error

**Solution**:
```csharp
// Test with a known endpoint first
bool success = await GalleryManager.UploadImageAsync(
    imagePath,
    "https://httpbin.org/post"  // Test endpoint
);

// Check returned bool and log for details
if (!success)
    Debug.LogError("Upload failed - check network and URL");
```

### Texture Is Blank

**Cause**: Invalid image path or corrupted file

**Solution**:
```csharp
if (!System.IO.File.Exists(imagePath))
{
    Debug.LogError("Image file not found");
    return;
}

var texture = await GalleryManager.GetImageTextureAsync(imagePath);
if (texture == null)
    Debug.LogError("Failed to load texture");
```

### Permission Request Not Showing

**Cause**: Permission already denied - user must enable in Settings

**Solution**:
- iOS/Android: Go to device Settings → Apps → Gallery App → Permissions
- For user guidance, check `GalleryManager.HasGalleryPermission()` and show appropriate dialog

### Build Error on Android

**Cause**: minSdkVersion too low or missing Activity

**Solution**:
- Set minSdkVersion to 21 or higher in build.gradle
- Verify `GalleryActivity` is in final AndroidManifest.xml

### Build Error on iOS

**Cause**: Missing frameworks or .mm file not compiled

**Solution**:
- Verify all frameworks are linked in Xcode: Photos, UIKit, Foundation
- Check that BumiGalleryBridge.mm is in Xcode project compilation target

---

## Architecture Notes

### Design Patterns

- **Static Utility**: Direct method calls, no instance needed
- **Async/Await**: All operations are non-blocking
- **Platform Abstraction**: Seamless iOS/Android handling via #if directives
- **Native Bridges**: C# communicates with native code via bridges

### Data Flow

```
C# OpenGalleryAsync()
    ↓
Platform detection (#if UNITY_IOS/ANDROID)
    ├→ iOS: IOSGalleryWrapper → C Bridge → Swift → Photo Picker
    └→ Android: AndroidGalleryWrapper → Java → Gallery Intent
         ↓
    User selects image
         ↓
    Native → UnitySendMessage()
         ↓
    C# Callback → TaskCompletionSource resolved
         ↓
    Return image path to caller
```

---

## See Also

- [README.md](README.md) - Quick overview and setup
- [CHANGELOG.md](CHANGELOG.md) - Version history
- [Example Code](Runtime/Scripts/Examples/GalleryExample.cs) - Working example

---

### IGalleryService (Interface)

Defines the contract for gallery operations. Useful for dependency injection or custom implementations.

```csharp
public interface IGalleryService
{
    Task<string> OpenGalleryAsync();
    Task<bool> UploadImageAsync(string imagePath, string uploadUrl, string fieldName = "image");
    Task<Texture2D> GetImageTextureAsync(string imagePath);
    bool HasGalleryPermission();
    Task<bool> RequestGalleryPermissionAsync();
}
```

---

## Script Usage

### Core Scripts

#### 1. GalleryManager.cs

The main singleton manager and entry point for all gallery operations.

**Purpose:**

- Provides unified API for gallery access
- Handles platform detection and routing
- Manages singleton lifecycle

**Key Features:**

- Singleton pattern (`GalleryManager.Instance`)
- Implements `IGalleryService` interface
- Automatic platform detection
- Error handling and logging

**Example Usage:**

```csharp
public class ProfileUploader : MonoBehaviour
{
    async void UploadProfilePicture()
    {
        var gallery = GalleryManager.Instance;

        // Check permission first
        if (!gallery.HasGalleryPermission())
        {
            bool granted = await gallery.RequestGalleryPermissionAsync();
            if (!granted) return;
        }

        // Open gallery
        string imagePath = await gallery.OpenGalleryAsync();

        // Upload if selected
        if (!string.IsNullOrEmpty(imagePath))
        {
            bool success = await gallery.UploadImageAsync(
                imagePath,
                "https://api.myapp.com/profile/upload"
            );
        }
    }
}
```

#### 2. IOSGalleryWrapper.cs

Platform-specific implementation for iOS.

**Purpose:**

- Bridges C# with Swift native code
- Handles iOS-specific callbacks
- Manages iOS permission flow

**Platform:** iOS only (compiled with `#if UNITY_IOS`)

#### 3. AndroidGalleryWrapper.cs

Platform-specific implementation for Android.

**Purpose:**

- Bridges C# with Java native code
- Handles Android intents and callbacks
- Manages Android permission flow

**Platform:** Android only (compiled with `#if UNITY_ANDROID`)

#### 4. IGalleryService.cs

Interface definition for gallery service.

**Purpose:**

- Defines public API contract
- Enables dependency injection
- Allows custom implementations

#### 5. GalleryExample.cs (Examples/)

Complete working example with UI integration.

**Purpose:**

- Demonstrates full workflow
- Shows UI integration patterns
- Provides copy-paste ready code

**Features:**

- Button handlers for gallery operations
- Image preview display
- Permission handling
- Upload with progress feedback

---

## Setup Guide

### Platform-Specific Configuration

#### iOS Setup

**1. Info.plist Configuration**

Add these keys to your `Info.plist` (required for App Store submission):

```xml
<key>NSPhotoLibraryUsageDescription</key>
<string>We need access to your photo library to upload images.</string>

<key>NSPhotoLibraryAddOnlyUsageDescription</key>
<string>We need permission to save images to your photo library.</string>
```

**2. Framework Linkage**

In Xcode, ensure these frameworks are linked (Build Phases → Link Binary With Libraries):

- `Photos.framework`
- `UIKit.framework`
- `Foundation.framework`

**3. Build Settings**

- Minimum iOS version: 12.0
- Swift Language Version: 5.0+

**4. Native Code Files**

- `BumiGalleryManager.swift` - Main Swift implementation
- `BumiGalleryBridge.h` - C# to Swift bridge header

---

#### Android Setup

**1. AndroidManifest.xml Permissions**

The following permissions are automatically included from the package:

```xml
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.READ_MEDIA_IMAGES" />
<uses-permission android:name="android.permission.INTERNET" />
```

**2. Gradle Configuration**

Ensure your `build.gradle` has:

```gradle
android {
    compileSdkVersion 33

    defaultConfig {
        minSdkVersion 21
        targetSdkVersion 33
    }
}
```

**3. Dependencies**

Required libraries (included automatically):

- `androidx.core:core:1.6.0+`
- Standard Android SDK libraries

**4. Native Code Files**

- `GalleryManager.java` - Main Java implementation
- `AndroidManifest.xml` - Permission declarations

---

### Runtime Setup

**1. Unity Package Installation**

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.bumimobile.gallery": "0.1.0",
    "com.bumimobile.core": "^0.1.1"
  }
}
```

**2. Verify Installation**

Check that these files are present:

- `Packages/com.bumimobile.gallery/Runtime/Scripts/GalleryManager.cs`
- `Packages/com.bumimobile.gallery/Runtime/Plugins/iOS/`
- `Packages/com.bumimobile.gallery/Runtime/Plugins/Android/`

---

## Integration Patterns

### Pattern 1: Simple Image Selection

```csharp
using BumiMobile.Gallery;
using UnityEngine;

public class SimpleGallery : MonoBehaviour
{
    async void SelectImage()
    {
        var imagePath = await GalleryManager.Instance.OpenGalleryAsync();

        if (!string.IsNullOrEmpty(imagePath))
        {
            Debug.Log($"Selected: {imagePath}");
        }
    }
}
```

### Pattern 2: Select, Preview, and Upload

```csharp
using BumiMobile.Gallery;
using UnityEngine;
using UnityEngine.UI;

public class ProfilePhotoUploader : MonoBehaviour
{
    [SerializeField] private RawImage previewImage;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button uploadButton;

    private string selectedImagePath;

    void Start()
    {
        selectButton.onClick.AddListener(SelectPhoto);
        uploadButton.onClick.AddListener(UploadPhoto);
    }

    async void SelectPhoto()
    {
        var gallery = GalleryManager.Instance;

        // Request permission if needed
        if (!gallery.HasGalleryPermission())
        {
            bool granted = await gallery.RequestGalleryPermissionAsync();
            if (!granted)
            {
                Debug.LogError("Permission denied");
                return;
            }
        }

        // Open gallery
        selectedImagePath = await gallery.OpenGalleryAsync();

        // Show preview
        if (!string.IsNullOrEmpty(selectedImagePath))
        {
            Texture2D texture = await gallery.GetImageTextureAsync(selectedImagePath);
            previewImage.texture = texture;
            uploadButton.interactable = true;
        }
    }

    async void UploadPhoto()
    {
        if (string.IsNullOrEmpty(selectedImagePath)) return;

        bool success = await GalleryManager.Instance.UploadImageAsync(
            selectedImagePath,
            "https://api.myapp.com/upload",
            "profile_photo"
        );

        if (success)
        {
            Debug.Log("Upload successful!");
        }
    }
}
```

### Pattern 3: Permission Flow

```csharp
using BumiMobile.Gallery;
using UnityEngine;

public class PermissionHandler : MonoBehaviour
{
    async void RequestGalleryAccess()
    {
        var gallery = GalleryManager.Instance;

        // Check current permission status
        if (gallery.HasGalleryPermission())
        {
            Debug.Log("Already have permission");
            return;
        }

        // Request permission
        bool granted = await gallery.RequestGalleryPermissionAsync();

        if (granted)
        {
            Debug.Log("Permission granted!");
            // Proceed with gallery operations
        }
        else
        {
            Debug.Log("Permission denied. Please enable in Settings.");
            // Show dialog directing user to Settings
        }
    }
}
```

### Pattern 4: Error Handling

```csharp
using BumiMobile.Gallery;
using UnityEngine;
using System;

public class RobustGalleryHandler : MonoBehaviour
{
    async void SafeGalleryOperation()
    {
        try
        {
            var gallery = GalleryManager.Instance;

            // Validate permission
            if (!gallery.HasGalleryPermission())
            {
                bool granted = await gallery.RequestGalleryPermissionAsync();
                if (!granted)
                {
                    ShowError("Gallery permission is required");
                    return;
                }
            }

            // Open gallery
            string imagePath = await gallery.OpenGalleryAsync();

            if (string.IsNullOrEmpty(imagePath))
            {
                Debug.Log("User cancelled selection");
                return;
            }

            // Validate file exists
            if (!System.IO.File.Exists(imagePath))
            {
                ShowError("Selected file not found");
                return;
            }

            // Upload with timeout
            bool success = await gallery.UploadImageAsync(
                imagePath,
                "https://api.myapp.com/upload"
            );

            if (success)
            {
                ShowSuccess("Upload completed!");
            }
            else
            {
                ShowError("Upload failed. Please try again.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Gallery operation failed: {ex.Message}");
            ShowError("An unexpected error occurred");
        }
    }

    void ShowError(string message) { /* Show UI error */ }
    void ShowSuccess(string message) { /* Show UI success */ }
}
```

---

## Troubleshooting

### Common Issues and Solutions

| Issue                          | Possible Cause               | Solution                                                    |
| ------------------------------ | ---------------------------- | ----------------------------------------------------------- |
| Gallery won't open             | Missing permissions          | Check `Info.plist` (iOS) or `AndroidManifest.xml` (Android) |
| Permission request not showing | Permission already denied    | User must enable in device Settings                         |
| Upload fails                   | Invalid URL or network issue | Verify URL, check network connectivity                      |
| Texture is blank               | File path invalid            | Verify file exists at the returned path                     |
| iOS build error                | Missing frameworks           | Link `Photos.framework` in Xcode                            |
| Android build error            | minSdkVersion too low        | Set `minSdkVersion` to 21 or higher                         |
| Crash on Android 13+           | Missing new permission       | Ensure `READ_MEDIA_IMAGES` permission                       |

### Debug Tips

**Enable Detailed Logging:**

```csharp
// GalleryManager logs to Unity Console by default
// Check for error messages after operations
```

**Test Permission Flow:**

```csharp
void TestPermissions()
{
    var hasPermission = GalleryManager.Instance.HasGalleryPermission();
    Debug.Log($"Has permission: {hasPermission}");
}
```

**Validate Upload URL:**

```csharp
async void TestUpload()
{
    // Test with a known working endpoint first
    var success = await GalleryManager.Instance.UploadImageAsync(
        testImagePath,
        "https://httpbin.org/post"  // Test endpoint
    );
}
```

### Platform-Specific Debugging

**iOS:**

- Check Xcode console for Swift errors
- Verify `Info.plist` entries are present
- Ensure device has photos in gallery
- Test on physical device (simulator limited)

**Android:**

- Use `adb logcat` to see Java errors
- Verify permissions in device Settings → Apps → Your App → Permissions
- Test on API 21+ device
- Check if gallery app is installed

---

## Architecture Notes

### Design Patterns Used

- **Singleton Pattern** - `GalleryManager.Instance` for easy global access
- **Async/Await** - Non-blocking operations with `TaskCompletionSource`
- **Interface Segregation** - Clean `IGalleryService` contract
- **Platform Abstraction** - Seamless platform switching via `#if` directives
- **Native Bridge Pattern** - Clean C# ↔ Native code communication

### Data Flow

```
User Action
    ↓
GalleryManager (Singleton)
    ↓
Platform Detection (#if UNITY_IOS/ANDROID)
    ├→ IOSGalleryWrapper → Swift → iOS PHPhotoLibrary
    └→ AndroidGalleryWrapper → Java → Android MediaStore
         ↓
    Callback via TaskCompletionSource
         ↓
    Return to C# caller
```

---

## Additional Resources

- See [README.md](README.md) for quick overview
- See [CHANGELOG.md](CHANGELOG.md) for version history
- Check `Runtime/Scripts/Examples/GalleryExample.cs` for complete working example
  OpenGalleryAsync()
  ↓
  TaskCompletionSource created
  ↓
  Native picker shown (non-blocking)
  ↓
  User selects image
  ↓
  Native code calls callback
  ↓
  TaskCompletionSource resolved
  ↓
  Result returned to caller

````

---

## 🔐 Permissions Handled

### iOS
- `NSPhotoLibraryUsageDescription` (Info.plist)
- Automatic PHPhotoLibrary permission request
- Permission status checking

### Android
- `android.permission.READ_EXTERNAL_STORAGE` (Android < 13)
- `android.permission.READ_MEDIA_IMAGES` (Android 13+)
- `android.permission.INTERNET` (for uploads)
- Runtime permission requests (Android 6.0+)

---

## 📚 Documentation Navigation

| Need | File |
|------|------|
| 5-minute setup | **QUICKSTART.md** |
| Platform setup | **SETUP.md** |
| All API methods | **API.md** |
| Integration patterns | **INTEGRATION.md** |
| Architecture details | **STRUCTURE.md** |
| What's new | **CHANGELOG.md** |

---

## 🎮 Example Usage Patterns

### Pattern 1: Simple Selection
```csharp
var path = await GalleryManager.Instance.OpenGalleryAsync();
````

### Pattern 2: With Permission Check

```csharp
if (!GalleryManager.Instance.HasGalleryPermission())
    await GalleryManager.Instance.RequestGalleryPermissionAsync();

var path = await GalleryManager.Instance.OpenGalleryAsync();
```

### Pattern 3: Full Workflow

```csharp
// Check permission
if (!GalleryManager.Instance.HasGalleryPermission())
{
    bool granted = await GalleryManager.Instance.RequestGalleryPermissionAsync();
    if (!granted) return;
}

// Select image
var imagePath = await GalleryManager.Instance.OpenGalleryAsync();
if (imagePath == null) return;

// Preview
var texture = await GalleryManager.Instance.GetImageTextureAsync(imagePath);
previewImage.sprite = Sprite.Create(texture, ...);

// Upload
bool success = await GalleryManager.Instance.UploadImageAsync(imagePath, serverUrl);
```

---

## 🔧 API Summary

```csharp
// Open gallery and select image
Task<string> OpenGalleryAsync()

// Upload image to server
Task<bool> UploadImageAsync(string imagePath, string uploadUrl, string fieldName = "image")

// Load image as Texture2D
Task<Texture2D> GetImageTextureAsync(string imagePath)

// Check if permission granted
bool HasGalleryPermission()

// Request permission from user
Task<bool> RequestGalleryPermissionAsync()
```

---

## ✨ Special Features

### Error Handling

- Comprehensive try-catch blocks
- Null/empty validation
- Detailed console logging
- Graceful fallbacks

### Performance

- Async operations (non-blocking UI)
- Image compression support
- Efficient file caching
- Network upload with multipart forms

### Security

- HTTPS support ready
- File validation capabilities
- Size limit checking
- Permission enforcement

---

## 📋 Implementation Checklist

- [x] C# wrapper scripts created
- [x] iOS Swift implementation with PHPhotoLibrary
- [x] Android Java implementation with MediaStore
- [x] Permission handling for both platforms
- [x] Image upload with multipart forms
- [x] Texture2D conversion support
- [x] Async/await pattern implementation
- [x] Example scene with UI
- [x] Complete documentation
- [x] API reference guide
- [x] Setup instructions
- [x] Error handling
- [x] Platform detection (#if directives)
- [x] Singleton pattern
- [x] Interface contracts

---

## 🚢 Ready for Production

This package includes everything needed for production:

- ✅ Full source code
- ✅ Native implementations
- ✅ Complete documentation
- ✅ Working examples
- ✅ Error handling
- ✅ Permission management
- ✅ MIT License

---

## 📝 Next Steps

1. **Review** the README.md for overview
2. **Read** QUICKSTART.md to start in 5 minutes
3. **Follow** SETUP.md for iOS/Android configuration
4. **Reference** API.md for all available methods
5. **Study** GalleryExample.cs for working implementation
6. **Test** on iOS and Android devices

---

## 📂 File Location

**All files are in:**

```
d:\Bumi\BumiMobileCore\Packages\com.bumimobile.gallery\
```

---

## 🎯 You're All Set!

Your Bumi Mobile Gallery package is **complete and ready to use**.

Simply follow the QUICKSTART.md and you'll have gallery functionality in your game within 5 minutes!

---

**Questions?** Refer to:

- API.md for method documentation
- SETUP.md for platform setup
- INTEGRATION.md for integration patterns
- GalleryExample.cs for working code

**Happy coding!** 🚀
