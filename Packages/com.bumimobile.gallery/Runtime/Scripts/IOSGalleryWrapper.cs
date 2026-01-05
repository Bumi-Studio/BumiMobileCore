using System.Threading.Tasks;
using UnityEngine;

namespace BumiMobile
{
    /// <summary>
    /// iOS platform-specific gallery operations wrapper.
    /// Handles communication with native iOS code via DllImport.
    /// </summary>
    public static class IOSGalleryWrapper
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern bool _CheckGalleryPermission();

        [DllImport("__Internal")]
        private static extern void _RequestGalleryPermission(string callbackGameObject, string callbackMethod);

        [DllImport("__Internal")]
        private static extern void _OpenGallery(string callbackGameObject, string callbackMethod);

        [DllImport("__Internal")]
        private static extern void _UploadImage(string imagePath, string uploadUrl, string fieldName, string callbackGameObject, string callbackMethod);
#endif

        private static TaskCompletionSource<string> _galleryTaskCompletionSource;
        private static TaskCompletionSource<bool> _permissionTaskCompletionSource;
        private static TaskCompletionSource<bool> _uploadTaskCompletionSource;

        /// <summary>
        /// Opens the native iOS photo picker.
        /// </summary>
        public static async Task<string> OpenGalleryAsync()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _galleryTaskCompletionSource = new TaskCompletionSource<string>();
            
            var go = new GameObject("IOSGalleryCallback");
            var callback = go.AddComponent<IOSGalleryCallback>();
            
            _OpenGallery(go.name, nameof(IOSGalleryCallback.OnGalleryImageSelected));
            
            return await _galleryTaskCompletionSource.Task;
#else
            Debug.LogWarning("iOS Gallery is only available on iOS platform.");
            return null;
#endif
        }

        /// <summary>
        /// Uploads an image on iOS.
        /// </summary>
        public static async Task<bool> UploadImageAsync(string imagePath, string uploadUrl, string fieldName)
        {
#if UNITY_IOS && !UNITY_EDITOR
            _uploadTaskCompletionSource = new TaskCompletionSource<bool>();
            
            var go = new GameObject("IOSUploadCallback");
            var callback = go.AddComponent<IOSGalleryCallback>();
            
            _UploadImage(imagePath, uploadUrl, fieldName, go.name, nameof(IOSGalleryCallback.OnUploadComplete));
            
            return await _uploadTaskCompletionSource.Task;
#else
            Debug.LogWarning("iOS Upload is only available on iOS platform.");
            return false;
#endif
        }

        /// <summary>
        /// Checks if gallery permission is granted on iOS.
        /// </summary>
        public static bool HasGalleryPermission()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return _CheckGalleryPermission();
#else
            return true;
#endif
        }

        /// <summary>
        /// Requests gallery permission on iOS.
        /// </summary>
        public static async Task<bool> RequestGalleryPermissionAsync()
        {
#if UNITY_IOS && !UNITY_EDITOR
            _permissionTaskCompletionSource = new TaskCompletionSource<bool>();
            
            var go = new GameObject("IOSPermissionCallback");
            var callback = go.AddComponent<IOSGalleryCallback>();
            
            _RequestGalleryPermission(go.name, nameof(IOSGalleryCallback.OnPermissionResult));
            
            return await _permissionTaskCompletionSource.Task;
#else
            return true;
#endif
        }

        // Callback methods called from native code
        public static void SetGalleryResult(string imagePath)
        {
            _galleryTaskCompletionSource?.TrySetResult(imagePath);
        }

        public static void SetPermissionResult(bool granted)
        {
            _permissionTaskCompletionSource?.TrySetResult(granted);
        }

        public static void SetUploadResult(bool success)
        {
            _uploadTaskCompletionSource?.TrySetResult(success);
        }
    }

    /// <summary>
    /// Callback handler for iOS gallery operations.
    /// </summary>
    public class IOSGalleryCallback : MonoBehaviour
    {
        public void OnGalleryImageSelected(string imagePath)
        {
            IOSGalleryWrapper.SetGalleryResult(imagePath);
            Destroy(gameObject);
        }

        public void OnPermissionResult(string result)
        {
            bool granted = result.Equals("granted", System.StringComparison.OrdinalIgnoreCase);
            IOSGalleryWrapper.SetPermissionResult(granted);
            Destroy(gameObject);
        }

        public void OnUploadComplete(string result)
        {
            bool success = result.Equals("success", System.StringComparison.OrdinalIgnoreCase);
            IOSGalleryWrapper.SetUploadResult(success);
            Destroy(gameObject);
        }
    }
}
