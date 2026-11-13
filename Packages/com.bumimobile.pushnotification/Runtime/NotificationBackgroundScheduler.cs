using System;
using UnityEngine;

/// <summary>
/// Schedules notifications when the app goes to the background and clears them when it returns,
/// so retention pushes are only delivered to players who actually stay away.
/// </summary>
public sealed class NotificationBackgroundScheduler : MonoBehaviour
{
    private static NotificationBackgroundScheduler _instance;

    /// <summary>When the current foreground session started (app launch or last resume).</summary>
    public static DateTime SessionStartedAt { get; private set; } = DateTime.Now;

    internal static void Create()
    {
        if (_instance != null)
        {
            return;
        }

        var host = new GameObject(nameof(NotificationBackgroundScheduler));
        DontDestroyOnLoad(host);
        _instance = host.AddComponent<NotificationBackgroundScheduler>();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            NotificationManager.ScheduleAllNotifications();
            return;
        }

        SessionStartedAt = DateTime.Now;
        NotificationManager.ClearScheduledNotifications();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
