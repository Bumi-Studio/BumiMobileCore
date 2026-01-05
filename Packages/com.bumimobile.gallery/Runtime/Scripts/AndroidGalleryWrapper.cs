using System.Threading.Tasks;
using UnityEngine;

namespace BumiMobile
{
    /// <summary>
    /// Android platform-specific gallery operations wrapper.
    /// Handles communication with native Android code via AndroidJavaClass/AndroidJavaObject.
    /// </summary>
    public static class AndroidGalleryWrapper
    {
        private const string GalleryClassPath = "com.bumimobile.gallery.GalleryManager";
        private static AndroidJavaClass _galleryClass;

        private static TaskCompletionSource<string> _galleryTaskCompletionSource;
        private static TaskCompletionSource<bool> _permissionTaskCompletionSource;
        private static TaskCompletionSource<bool> _uploadTaskCompletionSource;

        static AndroidGalleryWrapper()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                _galleryClass = new AndroidJavaClass(GalleryClassPath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to initialize Android Gallery: {ex.Message}");
            }
#endif
        }

        /// <summary>
        /// Opens the native Android file picker for images.
        /// </summary>
        public static async Task<string> OpenGalleryAsync()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_galleryClass == null)
            {
                Debug.LogError("Android Gallery class not initialized.");
                return null;
            }

            _galleryTaskCompletionSource = new TaskCompletionSource<string>();

            try
            {
                // Create callback receiver
                var callbackGameObject = new GameObject("AndroidGalleryCallback");
                var callback = callbackGameObject.AddComponent<AndroidGalleryCallback>();
                
                _galleryClass.CallStatic("openGallery", callbackGameObject.name, nameof(AndroidGalleryCallback.OnImageSelected));

                return await _galleryTaskCompletionSource.Task;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error opening Android gallery: {ex.Message}");
                return null;
            }
#else
            Debug.LogWarning("Android Gallery is only available on Android platform.");
            return null;
#endif
        }

        /// <summary>
        /// Uploads an image on Android.
        /// </summary>
        public static async Task<bool> UploadImageAsync(string imagePath, string uploadUrl, string fieldName)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_galleryClass == null)
            {
                Debug.LogError("Android Gallery class not initialized.");
                return false;
            }

            _uploadTaskCompletionSource = new TaskCompletionSource<bool>();

            try
            {
                // Create callback receiver
                var callbackGameObject = new GameObject("AndroidUploadCallback");
                var callback = callbackGameObject.AddComponent<AndroidGalleryCallback>();
                
                _galleryClass.CallStatic("uploadImage", imagePath, uploadUrl, fieldName, callbackGameObject.name, nameof(AndroidGalleryCallback.OnUploadComplete));
                
                return await _uploadTaskCompletionSource.Task;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error uploading image on Android: {ex.Message}");
                return false;
            }
#else
            Debug.LogWarning("Android Upload is only available on Android platform.");
            return false;
#endif
        }

        /// <summary>
        /// Checks if gallery permission is granted on Android.
        /// </summary>
        public static bool HasGalleryPermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_galleryClass == null)
            {
                return false;
            }

            try
            {
                return _galleryClass.CallStatic<bool>("hasPermission");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error checking Android gallery permission: {ex.Message}");
                return false;
            }
#else
            return true;
#endif
        }

        /// <summary>
        /// Requests gallery permission on Android.
        /// </summary>
        public static async Task<bool> RequestGalleryPermissionAsync()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_galleryClass == null)
            {
                return false;
            }

            _permissionTaskCompletionSource = new TaskCompletionSource<bool>();

            try
            {
                // Create callback receiver
                var callbackGameObject = new GameObject("AndroidPermissionCallback");
                var callback = callbackGameObject.AddComponent<AndroidGalleryCallback>();
                
                _galleryClass.CallStatic("requestPermission", callbackGameObject.name, nameof(AndroidGalleryCallback.OnPermissionResult));
                
                return await _permissionTaskCompletionSource.Task;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error requesting Android gallery permission: {ex.Message}");
                return false;
            }
#else
            return true;
#endif
        }

        // Callback methods called from native Android code via UnitySendMessage
        public static void SetGalleryResult(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                _galleryTaskCompletionSource?.TrySetResult(null);
            }
            else
            {
                _galleryTaskCompletionSource?.TrySetResult(imagePath);
            }
        }

        public static void SetPermissionResult(string result)
        {
            bool granted = result == "granted";
            _permissionTaskCompletionSource?.TrySetResult(granted);
        }

        public static void SetUploadResult(string result)
        {
            bool success = result == "success";
            _uploadTaskCompletionSource?.TrySetResult(success);
        }
    }
}
