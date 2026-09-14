using UnityEngine;
using BumiMobile;


#if UNITY_ANDROID
using System.Collections;
using Unity.Notifications.Android;
#elif UNITY_IOS
using Unity.Notifications.iOS;
#endif

[RegisterModule("Notification Module", core: false, order: 999)]
public class NotificationInitModule : InitModule
{
    public override string ModuleName => "Notifications";

    [SerializeField]
    private NotificationSettings settings;

    public override void CreateComponent()
    {
        ApplyCatalogSettings();

#if UNITY_ANDROID
        Tween.InvokeCoroutine(RequestNotificationPermission());
#elif UNITY_IOS
        iOSNotificationCenter.RequestAuthorization(
            AuthorizationOption.Alert |
            AuthorizationOption.Badge |
            AuthorizationOption.Sound, true);
        ApplyPostPermissionSettings();
#else
        ApplyPostPermissionSettings();
#endif
    }

#if UNITY_ANDROID
    private IEnumerator RequestNotificationPermission()
    {
        var permissionRequest = new PermissionRequest();

        // Wait until the permission is granted or denied
        while (permissionRequest.Status == PermissionStatus.RequestPending)
        {
            yield return null;
        }

        if (permissionRequest.Status == PermissionStatus.Allowed)
        {
            Debug.Log("Notification permission granted.");
        }
        else
        {
            Debug.LogWarning("Notification permission denied.");
        }

        // Register Android notification channel after permission result
        var channel = new AndroidNotificationChannel()
        {
            Id = "default_channel",
            Name = "Default Channel",
            Importance = Importance.Default,
            Description = "Generic notifications",
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);

        // Optional: Clear previous notifications on app start
        AndroidNotificationCenter.CancelAllScheduledNotifications();

        ApplyPostPermissionSettings();
    }
#endif

    private void ApplyCatalogSettings()
    {
        if (settings != null)
        {
            settings.ApplyCatalog();
        }
        else
        {
            NotificationManager.RegisterCatalog(null);
        }
    }

    private void ApplyPostPermissionSettings()
    {
        if (settings == null)
        {
            return;
        }

        settings.ScheduleIfNeeded();
    }
}