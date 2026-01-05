using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BumiMobile
{
    /// <summary>
    /// Example usage of the Gallery Manager.
    /// Demonstrates opening gallery, selecting images, and uploading.
    /// </summary>
    public class GalleryExample : MonoBehaviour
    {
        [SerializeField] private Button openGalleryButton;
        [SerializeField] private Button uploadButton;
        [SerializeField] private Image imagePreview;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private string uploadServerUrl = "https://example.com/upload";

        private string selectedImagePath;

        private void Start()
        {
            if (openGalleryButton != null)
                openGalleryButton.onClick.AddListener(OnOpenGalleryClicked);

            if (uploadButton != null)
                uploadButton.onClick.AddListener(OnUploadClicked);

            UpdateStatusText("Ready to select image");
        }

        /// <summary>
        /// Called when user clicks "Open Gallery" button.
        /// </summary>
        private async void OnOpenGalleryClicked()
        {
            UpdateStatusText("Opening gallery...");

            var imagePath = await GalleryManager.Instance.OpenGalleryAsync();

            if (string.IsNullOrEmpty(imagePath))
            {
                UpdateStatusText("No image selected");
                return;
            }

            selectedImagePath = imagePath;
            UpdateStatusText($"Image selected: {System.IO.Path.GetFileName(imagePath)}");

            // Load and display the image
            await LoadImagePreview(imagePath);
        }

        /// <summary>
        /// Called when user clicks "Upload Image" button.
        /// </summary>
        private async void OnUploadClicked()
        {
            if (string.IsNullOrEmpty(selectedImagePath))
            {
                UpdateStatusText("Please select an image first");
                return;
            }

            UpdateStatusText("Uploading image...");
            var success = await GalleryManager.Instance.UploadImageAsync(
                selectedImagePath,
                uploadServerUrl,
                "image"
            );

            if (success)
            {
                UpdateStatusText("Image uploaded successfully!");
            }
            else
            {
                UpdateStatusText("Failed to upload image. Check console for details.");
            }
        }

        /// <summary>
        /// Loads and displays the image preview.
        /// </summary>
        private async System.Threading.Tasks.Task LoadImagePreview(string imagePath)
        {
            var texture = await GalleryManager.Instance.GetImageTextureAsync(imagePath);

            if (texture != null && imagePreview != null)
            {
                var sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
                imagePreview.sprite = sprite;
                imagePreview.SetNativeSize();
            }
        }

        private void UpdateStatusText(string message)
        {
            if (statusText != null)
                statusText.text = message;

            Debug.Log($"[Gallery] {message}");
        }

        private void OnDestroy()
        {
            if (openGalleryButton != null)
                openGalleryButton.onClick.RemoveListener(OnOpenGalleryClicked);

            if (uploadButton != null)
                uploadButton.onClick.RemoveListener(OnUploadClicked);
        }
    }
}
