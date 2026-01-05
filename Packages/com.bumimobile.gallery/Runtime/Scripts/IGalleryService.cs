using System;
using System.Threading.Tasks;
using UnityEngine;

namespace BumiMobile
{
    /// <summary>
    /// Interface for gallery operations across different platforms.
    /// </summary>
    public interface IGalleryService
    {
        /// <summary>
        /// Opens the native gallery picker to select an image.
        /// </summary>
        /// <returns>Path to the selected image file.</returns>
        Task<string> OpenGalleryAsync();

        /// <summary>
        /// Uploads an image to the specified URL.
        /// </summary>
        /// <param name="imagePath">Local path to the image file.</param>
        /// <param name="uploadUrl">Server URL to upload the image to.</param>
        /// <param name="fieldName">Form field name for the image (default: "image").</param>
        /// <returns>True if upload was successful.</returns>
        Task<bool> UploadImageAsync(string imagePath, string uploadUrl, string fieldName = "image");

        /// <summary>
        /// Gets the image as a Texture2D after selection.
        /// </summary>
        /// <param name="imagePath">Path to the image file.</param>
        /// <returns>Texture2D of the image.</returns>
        Task<Texture2D> GetImageTextureAsync(string imagePath);

        /// <summary>
        /// Checks if the gallery access permission is granted.
        /// </summary>
        bool HasGalleryPermission();

        /// <summary>
        /// Requests gallery access permission from the user.
        /// </summary>
        Task<bool> RequestGalleryPermissionAsync();
    }
}
