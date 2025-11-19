using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#elif UNITY_IOS
using Unity.Notifications.iOS;
#endif

public static class NotificationManager
{
    private const string DefaultChannelId = "default_channel";

    private static NotificationTemplateCatalog _catalog;

    public static NotificationTemplateCatalog Catalog => LoadCatalog();

    public static void RegisterCatalog(NotificationTemplateCatalog customCatalog)
    {
        _catalog = customCatalog;
        _catalog?.RemoveNullEntries();
    }

    public static IReadOnlyList<ScheduledNotification> ScheduleAllNotifications(NotificationScheduleContext context = null)
    {
        context ??= new NotificationScheduleContext();

        ClearScheduledNotifications();

        var catalog = Catalog;
        if (catalog == null || catalog.Notifications == null || catalog.Notifications.Count == 0)
        {
            Debug.LogWarning("[NotificationManager] No notification templates configured.");
            return Array.Empty<ScheduledNotification>();
        }

        var scheduled = new List<ScheduledNotification>();
        foreach (var template in catalog.Notifications)
        {
            if (template == null)
            {
                continue;
            }
            foreach (var candidate in DetermineSchedule(template, context))
            {
                SchedulePlatformNotification(candidate);
                scheduled.Add(candidate);
                context.AppendHistory(candidate.Type, candidate.FireTime);
            }
        }

        return scheduled;
    }

        public static void ClearScheduledNotifications()
        {
    #if UNITY_ANDROID
        AndroidNotificationCenter.CancelAllScheduledNotifications();
    #elif UNITY_IOS
        iOSNotificationCenter.RemoveAllScheduledNotifications();
    #else
        // No-op outside of supported platforms.
    #endif
        }

    public static ScheduledNotification ScheduleNotification(NotificationType type, DateTime fireTime, bool repeatDaily = false)
    {
        var template = Catalog.Get(type);
        if (template == null)
        {
            Debug.LogWarning($"[NotificationManager] Template not found for notification type '{type}'.");
            return null;
        }

        var scheduled = CreateSchedule(template, fireTime, repeatDaily, DateTime.Now);
        SchedulePlatformNotification(scheduled);
        return scheduled;
    }

    private static IEnumerable<ScheduledNotification> DetermineSchedule(NotificationTemplate template, NotificationScheduleContext context)
    {
        if (template == null)
        {
            yield break;
        }

        var now = context?.Now ?? DateTime.Now;

        if (IsInCooldown(template, context, now))
        {
            yield break;
        }

        var slots = GetRemainingSlots(template, context, now);
        if (slots <= 0)
        {
            yield break;
        }

        var scheduledCount = 0;

        bool HasCapacity()
        {
            return slots == int.MaxValue || scheduledCount < slots;
        }

        switch (template.trigger.triggerType)
        {
            case NotificationTriggerType.DailyAtLocalTime:
                if (template.trigger.skipIfLoggedInToday && context?.HasLoggedInToday == true)
                {
                    yield break;
                }

                if (template.trigger.skipIfDailyRewardClaimed && context?.DailyRewardClaimedToday == true)
                {
                    yield break;
                }

                if (!HasCapacity())
                {
                    yield break;
                }

                var dailyFireTime = GetNextFixedTime(template.trigger, now);
                var dailySchedule = CreateSchedule(template, dailyFireTime, true, now);
                scheduledCount++;
                yield return dailySchedule;
                break;

            case NotificationTriggerType.InactivityThreshold:
                if (context?.LastLogin == null)
                {
                    yield break;
                }

                var hoursInactive = (now - context.LastLogin.Value).TotalHours;
                if (hoursInactive < template.trigger.inactivityThresholdHours)
                {
                    yield break;
                }

                if (!HasCapacity())
                {
                    yield break;
                }

                var inactivityFire = GetNextFixedTime(template.trigger, now);
                var inactivitySchedule = CreateSchedule(template, inactivityFire, false, now);
                scheduledCount++;
                yield return inactivitySchedule;
                break;

            case NotificationTriggerType.LevelStuck:
                if (context?.LevelStuckSince == null)
                {
                    yield break;
                }

                var stuckDuration = (now - context.LevelStuckSince.Value).TotalHours;
                if (stuckDuration < template.trigger.levelStuckThresholdHours)
                {
                    yield break;
                }

                if (template.trigger.reengagementBlockHours > 0 &&
                    HasRecentNotification(NotificationType.Reengagement, TimeSpan.FromHours(template.trigger.reengagementBlockHours), context, now))
                {
                    yield break;
                }

                if (!HasCapacity())
                {
                    yield break;
                }

                var levelFire = now.AddMinutes(Math.Max(template.trigger.eventDelayMinutes, 0));
                if (levelFire <= now)
                {
                    levelFire = now.AddMinutes(1);
                }

                var levelSchedule = CreateSchedule(template, levelFire, false, now);
                scheduledCount++;
                yield return levelSchedule;
                break;

            case NotificationTriggerType.FeatureEvent:
                if (context?.FeatureLaunchTime == null)
                {
                    yield break;
                }

                if (template.trigger.skipIfFeatureInteracted && context.HasInteractedWithLatestFeature)
                {
                    yield break;
                }

                if (!HasCapacity())
                {
                    yield break;
                }

                var featureFire = context.FeatureLaunchTime.Value.AddMinutes(Math.Max(template.trigger.eventDelayMinutes, 0));
                if (featureFire < now)
                {
                    featureFire = now.AddMinutes(1);
                }

                var featureSchedule = CreateSchedule(template, featureFire, false, now);
                scheduledCount++;
                yield return featureSchedule;
                break;

            case NotificationTriggerType.CompetitionLifecycle:
                if (context?.CompetitionStartTime == null || context.CompetitionEndTime == null)
                {
                    yield break;
                }

                if (template.trigger.includeStartEvent && HasCapacity())
                {
                    var startTime = context.CompetitionStartTime.Value;
                    if (startTime > now)
                    {
                        var startSchedule = CreateSchedule(template, startTime, false, now);
                        scheduledCount++;
                        yield return startSchedule;
                    }
                }

                if (template.trigger.includeLastCall && HasCapacity())
                {
                    var lastCallTime = context.CompetitionEndTime.Value.AddHours(-Math.Abs(template.trigger.competitionLastCallLeadHours));
                    if (lastCallTime > now)
                    {
                        var lastCallSchedule = CreateSchedule(template, lastCallTime, false, now);
                        scheduledCount++;
                        yield return lastCallSchedule;
                    }
                }

                break;
        }
    }

    private static NotificationTemplateCatalog LoadCatalog()
    {
        if (_catalog != null)
        {
            _catalog.RemoveNullEntries();
            return _catalog;
        }

        var assetCatalog = Resources.Load<NotificationTemplateCatalog>(NotificationTemplateCatalog.DefaultResourcePath);
        if (assetCatalog != null)
        {
            assetCatalog.RemoveNullEntries();
            if (assetCatalog.Notifications != null && assetCatalog.Notifications.Count > 0)
            {
                _catalog = assetCatalog;
                return _catalog;
            }

#if UNITY_EDITOR
            Debug.LogWarning("[NotificationManager] NotificationTemplateCatalog asset was found but contains no templates. Falling back to built-in defaults.");
#endif
        }

        _catalog = ScriptableObject.CreateInstance<NotificationTemplateCatalog>();
        _catalog.SetRuntimeTemplates(NotificationTemplateDefaults.CreateRuntimeTemplates());
#if UNITY_EDITOR
        Debug.LogWarning("[NotificationManager] Using built-in notification templates. Create a NotificationTemplateCatalog asset under a Resources folder to override.");
#endif
        return _catalog;
    }

    private static ScheduledNotification CreateSchedule(NotificationTemplate template, DateTime fireTime, bool repeatDaily, DateTime referenceNow)
    {
        if (fireTime <= referenceNow)
        {
            fireTime = referenceNow.AddMinutes(1);
        }

        return new ScheduledNotification(template, fireTime, repeatDaily, DefaultChannelId);
    }

    private static DateTime GetNextFixedTime(NotificationTriggerSettings trigger, DateTime reference)
    {
        var offset = ParseTimezoneOffset(trigger.timezone);
        var referenceOffset = new DateTimeOffset(reference, TimeZoneInfo.Local.GetUtcOffset(reference)).ToOffset(offset);
        var candidate = new DateTimeOffset(referenceOffset.Year, referenceOffset.Month, referenceOffset.Day, trigger.hour, trigger.minute, 0, offset);
        if (candidate <= referenceOffset)
        {
            candidate = candidate.AddDays(1);
        }

        return candidate.ToLocalTime().DateTime;
    }

    private static TimeSpan ParseTimezoneOffset(string timezone)
    {
        if (string.IsNullOrWhiteSpace(timezone))
        {
            return TimeSpan.Zero;
        }

        var trimmed = timezone.Trim().ToUpperInvariant();
        if (trimmed == "Z")
        {
            return TimeSpan.Zero;
        }

        if (trimmed.StartsWith("UTC", StringComparison.Ordinal) || trimmed.StartsWith("GMT", StringComparison.Ordinal))
        {
            trimmed = trimmed.Substring(3);
        }

        trimmed = trimmed.Trim();
        if (trimmed.Length == 0)
        {
            return TimeSpan.Zero;
        }

        var sign = 1;
        if (trimmed[0] == '+')
        {
            trimmed = trimmed.Substring(1);
        }
        else if (trimmed[0] == '-')
        {
            sign = -1;
            trimmed = trimmed.Substring(1);
        }

        var hourPart = trimmed;
        var minutePart = "0";

        var separatorIndex = trimmed.IndexOf(':');
        if (separatorIndex >= 0)
        {
            hourPart = trimmed.Substring(0, separatorIndex);
            minutePart = trimmed.Substring(separatorIndex + 1);
        }

        if (!int.TryParse(hourPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hours))
        {
            return TimeSpan.Zero;
        }

        var minutes = 0;
        if (!string.IsNullOrEmpty(minutePart))
        {
            int.TryParse(minutePart, NumberStyles.Integer, CultureInfo.InvariantCulture, out minutes);
        }

        hours = Math.Abs(hours);
        minutes = Math.Abs(minutes);

        return new TimeSpan(sign * hours, sign * minutes, 0);
    }

    private static bool IsInCooldown(NotificationTemplate template, NotificationScheduleContext context, DateTime now)
    {
        if (template.rules.cooldownHours <= 0 || context == null)
        {
            return false;
        }

        var history = context.GetHistory(template.type);
        if (history == null || history.Count == 0)
        {
            return false;
        }

        var threshold = now.AddHours(-template.rules.cooldownHours);
        return history.Any(t => t >= threshold);
    }

    private static int GetRemainingSlots(NotificationTemplate template, NotificationScheduleContext context, DateTime now)
    {
        if (template.rules.maxPerPeriod <= 0)
        {
            return int.MaxValue;
        }

        var window = GetPeriodWindow(template.rules);
        if (window == TimeSpan.Zero)
        {
            return template.rules.maxPerPeriod;
        }

        if (context == null)
        {
            return template.rules.maxPerPeriod;
        }

        var history = context.GetHistory(template.type);
        if (history == null || history.Count == 0)
        {
            return template.rules.maxPerPeriod;
        }

        var windowStart = now - window;
        var count = history.Count(t => t >= windowStart);
        var remaining = template.rules.maxPerPeriod - count;
        return remaining > 0 ? remaining : 0;
    }

    private static TimeSpan GetPeriodWindow(NotificationRules rules)
    {
        return rules.period switch
        {
            NotificationPeriod.Day => TimeSpan.FromDays(1),
            NotificationPeriod.Week => TimeSpan.FromDays(7),
            NotificationPeriod.Custom when rules.periodHoursOverride > 0 => TimeSpan.FromHours(rules.periodHoursOverride),
            _ => TimeSpan.Zero,
        };
    }

    private static bool HasRecentNotification(NotificationType type, TimeSpan window, NotificationScheduleContext context, DateTime now)
    {
        if (context == null || window <= TimeSpan.Zero)
        {
            return false;
        }

        var history = context.GetHistory(type);
        if (history == null || history.Count == 0)
        {
            return false;
        }

        var threshold = now - window;
        return history.Any(t => t >= threshold);
    }

    private static void SchedulePlatformNotification(ScheduledNotification scheduled)
    {
        if (scheduled == null)
        {
            return;
        }

#if UNITY_ANDROID
        var notification = new AndroidNotification
        {
            Title = scheduled.Template.message.title,
            Text = scheduled.Template.message.body,
            FireTime = scheduled.FireTime,
            SmallIcon = !string.IsNullOrEmpty(scheduled.Template.media.iconResource) ? scheduled.Template.media.iconResource : "icon_small"
        };

        if (!string.IsNullOrEmpty(scheduled.Template.media.largePictureResource))
        {
            notification.LargeIcon = scheduled.Template.media.largePictureResource;
        }

        notification.RepeatInterval = scheduled.RepeatDaily ? TimeSpan.FromDays(1) : (TimeSpan?)null;

        if (!string.IsNullOrEmpty(scheduled.Template.message.callToAction))
        {
            notification.IntentData = scheduled.Template.message.callToAction;
        }

        AndroidNotificationCenter.SendNotification(notification, scheduled.ChannelId);
#elif UNITY_IOS
        iOSNotificationTrigger trigger;
        if (scheduled.RepeatDaily)
        {
            trigger = new iOSNotificationTimeIntervalTrigger
            {
                TimeInterval = GetPositiveInterval(scheduled.FireTime - DateTime.Now),
                Repeats = true
            };
        }
        else
        {
            trigger = new iOSNotificationCalendarTrigger
            {
                Year = scheduled.FireTime.Year,
                Month = scheduled.FireTime.Month,
                Day = scheduled.FireTime.Day,
                Hour = scheduled.FireTime.Hour,
                Minute = scheduled.FireTime.Minute,
                Second = scheduled.FireTime.Second,
                Repeats = false
            };
        }

        var notification = new iOSNotification
        {
            Identifier = $"{scheduled.Type}_{scheduled.FireTime:yyyyMMddHHmmss}",
            Title = scheduled.Template.message.title,
            Body = scheduled.Template.message.body,
            ShowInForeground = true,
            ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound,
            Trigger = trigger
        };

        if (!string.IsNullOrEmpty(scheduled.Template.message.callToAction))
        {
            notification.UserInfo = new Dictionary<string, string>
            {
                { "cta", scheduled.Template.message.callToAction }
            };
        }

        iOSNotificationCenter.ScheduleNotification(notification);
#else
        Debug.Log($"[NotificationManager] Scheduled '{scheduled.Template.message.title}' for {scheduled.FireTime} (repeatDaily: {scheduled.RepeatDaily})");
#endif
    }

#if UNITY_IOS
    private static TimeSpan GetPositiveInterval(TimeSpan interval)
    {
        return interval <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : interval;
    }
#endif
}

public sealed class ScheduledNotification
{
    internal ScheduledNotification(NotificationTemplate template, DateTime fireTime, bool repeatDaily, string channelId)
    {
        Template = template;
        Type = template.type;
        FireTime = fireTime;
        RepeatDaily = repeatDaily;
        ChannelId = channelId;
    }

    public NotificationType Type { get; }
    public DateTime FireTime { get; internal set; }
    public bool RepeatDaily { get; }
    public NotificationTemplate Template { get; }
    public string ChannelId { get; }
}

public sealed class NotificationScheduleContext
{
    public DateTime Now { get; set; } = DateTime.Now;
    public DateTime? LastLogin { get; set; }
    public bool HasLoggedInToday { get; set; }
    public DateTime? LevelStuckSince { get; set; }
    public bool DailyRewardClaimedToday { get; set; }
    public bool HasInteractedWithLatestFeature { get; set; }
    public DateTime? FeatureLaunchTime { get; set; }
    public DateTime? CompetitionStartTime { get; set; }
    public DateTime? CompetitionEndTime { get; set; }

    private readonly Dictionary<NotificationType, List<DateTime>> history = new Dictionary<NotificationType, List<DateTime>>();

    public void SetHistory(NotificationType type, IEnumerable<DateTime> timestamps)
    {
        if (!history.TryGetValue(type, out var list))
        {
            list = new List<DateTime>();
            history[type] = list;
        }

        list.Clear();

        if (timestamps == null)
        {
            return;
        }

        list.AddRange(timestamps);
    }

    public void AppendHistory(NotificationType type, DateTime timestamp)
    {
        if (!history.TryGetValue(type, out var list))
        {
            list = new List<DateTime>();
            history[type] = list;
        }

        list.Add(timestamp);
    }

    public IReadOnlyList<DateTime> GetHistory(NotificationType type)
    {
        if (history.TryGetValue(type, out var list))
        {
            return list;
        }

        return Array.Empty<DateTime>();
    }
}