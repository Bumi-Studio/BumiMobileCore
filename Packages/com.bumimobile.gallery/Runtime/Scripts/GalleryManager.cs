using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_IOS
using UnityEngine.iOS;
#endif

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace BumiMobile
{
    /// <summary>
    /// Main gallery manager that provides cross-platform gallery and image upload functionality.
    /// </summary>
    public class GalleryManager : MonoBehaviour, IGalleryService
    {
        private static GalleryManager _instance;

        [SerializeField] private bool _autoCreateInstance = true;

        /// <summary>
        /// Gets the singleton instance of GalleryManager.
        /// </summary>
        public static GalleryManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var existing = FindObjectOfType<GalleryManager>();
                    if (existing != null)
                    {
                        _instance = existing;
                    }
                    else
                    {
                        var go = new GameObject("GalleryManager");
                        _instance = go.AddComponent<GalleryManager>();
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Opens the native gallery picker to select an image.
        /// </summary>
        public async Task<string> OpenGalleryAsync()
        {
            try
            {
                if (!HasGalleryPermission())
                {
                    var permissionGranted = await RequestGalleryPermissionAsync();
                    if (!permissionGranted)
                    {
                        Debug.LogWarning("Gallery permission was denied.");
                        return null;
                    }
                }

#if UNITY_IOS
                return await IOSGalleryWrapper.OpenGalleryAsync();
#elif UNITY_ANDROID
                return await AndroidGalleryWrapper.OpenGalleryAsync();
#else
                Debug.LogError("Gallery functionality is not supported on this platform.");
                return null;
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error opening gallery: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Uploads an image to the specified URL.
        /// </summary>
        public async Task<bool> UploadImageAsync(string imagePath, string uploadUrl, string fieldName = "image")
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                {
                    Debug.LogError($"Image file not found: {imagePath}");
                    return false;
                }

                if (string.IsNullOrEmpty(uploadUrl))
                {
                    Debug.LogError("Upload URL is not specified.");
                    return false;
                }

#if UNITY_IOS
                return await IOSGalleryWrapper.UploadImageAsync(imagePath, uploadUrl, fieldName);
#elif UNITY_ANDROID
                return await AndroidGalleryWrapper.UploadImageAsync(imagePath, uploadUrl, fieldName);
#else
                Debug.LogError("Upload functionality is not supported on this platform.");
                return false;
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error uploading image: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the image as a Texture2D from the specified path.
        /// </summary>
        public async Task<Texture2D> GetImageTextureAsync(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                {
                    Debug.LogError($"Image file not found: {imagePath}");
                    return null;
                }

                return await Task.Run(() =>
                {
                    var fileData = File.ReadAllBytes(imagePath);
                    var texture = new Texture2D(1, 1, TextureFormat.RGB24, false);
                    texture.LoadImage(fileData);
                    return texture;
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading texture: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Checks if gallery access permission is granted.
        /// </summary>
        public bool HasGalleryPermission()
        {
#if UNITY_IOS
            return IOSGalleryWrapper.HasGalleryPermission();
#elif UNITY_ANDROID
            return AndroidGalleryWrapper.HasGalleryPermission();
#else
            return true;
#endif
        }

        /// <summary>
        /// Requests gallery access permission from the user.
        /// </summary>
        public async Task<bool> RequestGalleryPermissionAsync()
        {
#if UNITY_IOS
            return await IOSGalleryWrapper.RequestGalleryPermissionAsync();
#elif UNITY_ANDROID
            return await AndroidGalleryWrapper.RequestGalleryPermissionAsync();
#else
            return true;
#endif
        }
    }
}
