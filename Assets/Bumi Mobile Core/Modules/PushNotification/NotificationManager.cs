using System;
using UnityEngine;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#elif UNITY_IOS
using Unity.Notifications.iOS;
#endif

public static class NotificationManager
{
    private static DateTime? lastScheduledTime = null;

    public static void ScheduleAllNotifications()
    {
        ScheduleDailyNotification("A", "This is notification A", 10, 0); // Every day at 10AM
        ScheduleNotificationAfterHours("B", "This is notification B", 2); // 2 hours after app closes
        ScheduleNotificationAtDate("C", "This is notification C", new DateTime(2025, 7, 17, 15, 0, 0)); // July 17th at 3PM
    }

    public static void ScheduleSafeAllNotifications()
    {
        ScheduleNotificationAfterHours("B", "This is notification B", 2); // 2 hours after close

        var notifCTime = new DateTime(2025, 7, 18, 19, 0, 0); // C at 7:00 PM
        SafeScheduleNotification("C", "This is notification C", notifCTime);

        var notifADate = new DateTime(2025, 7, 18, 19, 5, 0); // A at 7:05 PM on July 18
        if ((notifADate - notifCTime).TotalHours < 4)
        {
            notifADate = notifCTime.AddHours(4); // delay A to avoid conflict
        }

        // A for July 18 only (one-time with adjusted time)
        SafeScheduleNotification("A", "This is notification A", notifADate);

        // Daily A starting from July 19 at 4PM
        ScheduleDailyNotification("A", "This is notification A", 16, 0, startFrom: DateTime.Today.AddDays(1));
    }

    public static void ScheduleDailyNotification(string title, string text, int hour, int minute, DateTime? startFrom = null)
    {
#if UNITY_ANDROID
        var fireTime = GetNextTime(hour, minute);
        if (startFrom.HasValue && fireTime < startFrom.Value)
        {
            fireTime = new DateTime(startFrom.Value.Year, startFrom.Value.Month, startFrom.Value.Day, hour, minute, 0);
        }

        var notification = new AndroidNotification()
        {
            Title = title,
            Text = text,
            FireTime = fireTime,
            RepeatInterval = TimeSpan.FromDays(1),
            SmallIcon = "icon_small",
            LargeIcon = "icon_large"
        };

        AndroidNotificationCenter.SendNotification(notification, "default_channel");

#elif UNITY_IOS
        var timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = GetNextTime(hour, minute) - DateTime.Now,
            Repeats = true
        };

        var notification = new iOSNotification()
        {
            Identifier = title,
            Title = title,
            Body = text,
            ShowInForeground = true,
            ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
            Trigger = timeTrigger,
        };

        iOSNotificationCenter.ScheduleNotification(notification);
#endif
    }

    public static void ScheduleNotificationAfterHours(string title, string text, int hours)
    {
#if UNITY_ANDROID
        var notification = new AndroidNotification()
        {
            Title = title,
            Text = text,
            FireTime = DateTime.Now.AddHours(hours),
            SmallIcon = "icon_small",
        };
        AndroidNotificationCenter.SendNotification(notification, "default_channel");
#elif UNITY_IOS
        var notification = new iOSNotification()
        {
            Identifier = title,
            Title = title,
            Body = text,
            ShowInForeground = true,
            ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound,
            Trigger = new iOSNotificationTimeIntervalTrigger()
            {
                TimeInterval = TimeSpan.FromHours(hours),
                Repeats = false
            }
        };
        iOSNotificationCenter.ScheduleNotification(notification);
#endif
    }

    public static void ScheduleNotificationAtDate(string title, string text, DateTime time)
    {
#if UNITY_ANDROID
        var notification = new AndroidNotification()
        {
            Title = title,
            Text = text,
            FireTime = time,
            SmallIcon = "icon_small",
        };
        AndroidNotificationCenter.SendNotification(notification, "default_channel");
#elif UNITY_IOS
        var timeTrigger = new iOSNotificationCalendarTrigger()
        {
            Year = time.Year,
            Month = time.Month,
            Day = time.Day,
            Hour = time.Hour,
            Minute = time.Minute,
            Second = 0,
            Repeats = false
        };

        var notification = new iOSNotification()
        {
            Identifier = title,
            Title = title,
            Body = text,
            ShowInForeground = true,
            ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound,
            Trigger = timeTrigger
        };
        iOSNotificationCenter.ScheduleNotification(notification);
#endif
    }

    public static void SafeScheduleNotification(string title, string text, DateTime desiredTime)
    {
        if (lastScheduledTime.HasValue && (desiredTime - lastScheduledTime.Value).TotalHours < 4)
        {
            // Push at least 4 hours after the last one
            desiredTime = lastScheduledTime.Value.AddHours(4);
        }

        lastScheduledTime = desiredTime;

#if UNITY_ANDROID
        var notification = new AndroidNotification()
        {
            Title = title,
            Text = text,
            FireTime = desiredTime,
            SmallIcon = "icon_small",
            LargeIcon = "icon_large"
        };

        AndroidNotificationCenter.SendNotification(notification, "default_channel");
#elif UNITY_IOS
        var timeTrigger = new iOSNotificationCalendarTrigger()
        {
            Year = desiredTime.Year,
            Month = desiredTime.Month,
            Day = desiredTime.Day,
            Hour = desiredTime.Hour,
            Minute = desiredTime.Minute,
            Second = 0,
            Repeats = false
        };

        var notification = new iOSNotification()
        {
            Identifier = title,
            Title = title,
            Body = text,
            ShowInForeground = true,
            ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound,
            Trigger = timeTrigger
        };
        iOSNotificationCenter.ScheduleNotification(notification);
#endif
    }

    private static DateTime GetNextTime(int hour, int minute)
    {
        DateTime now = DateTime.Now;
        DateTime target = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);
        return (target > now) ? target : target.AddDays(1);
    }
}