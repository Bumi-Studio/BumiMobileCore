using UnityEngine;

namespace BumiMobile
{
  /// <summary>
  /// Callback handler for Android gallery operations.
  /// Receives callbacks from native Android code via UnitySendMessage.
  /// </summary>
  public class AndroidGalleryCallback : MonoBehaviour
  {
    /// <summary>
    /// Called when user selects an image from gallery.
    /// </summary>
    public void OnImageSelected(string imagePath)
    {
      AndroidGalleryWrapper.SetGalleryResult(imagePath);
      Destroy(gameObject);
    }

    /// <summary>
    /// Called when permission request is completed.
    /// </summary>
    public void OnPermissionResult(string result)
    {
      AndroidGalleryWrapper.SetPermissionResult(result);
      Destroy(gameObject);
    }

    /// <summary>
    /// Called when image upload is completed.
    /// </summary>
    public void OnUploadComplete(string result)
    {
      AndroidGalleryWrapper.SetUploadResult(result);
      Destroy(gameObject);
    }
  }
}
