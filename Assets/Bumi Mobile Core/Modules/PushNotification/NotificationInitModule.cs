using System;
using UnityEngine;
using System.Threading.Tasks;
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
    public override void CreateComponent()
    {
#if UNITY_ANDROID
        Tween.InvokeCoroutine(RequestNotificationPermission());
#elif UNITY_IOS
        iOSNotificationCenter.RequestAuthorization(
            AuthorizationOption.Alert |
            AuthorizationOption.Badge |
            AuthorizationOption.Sound, true);
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
    }
#endif
}