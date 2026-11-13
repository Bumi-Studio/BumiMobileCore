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
    private const int GlobalCooldownHours = 4;

    private static readonly int[] DefaultDailyRetentionDays = { 1, 2 };
    private static readonly int[] DefaultReengagementDays = { 3, 7 };

    private static NotificationTemplateCatalog _catalog;
    private static bool _enableDeduplication = true;
    private static System.Random _random = new System.Random();
    private static readonly HashSet<string> _idempotencyKeys = new HashSet<string>();

    public static NotificationTemplateCatalog Catalog => LoadCatalog();

    /// <summary>
    /// Supplies the player state when scheduling runs without an explicit context
    /// (the app moving to the background). Set by the game.
    /// </summary>
    public static Func<NotificationScheduleContext> ContextProvider { get; set; }

    public static void RegisterCatalog(NotificationTemplateCatalog customCatalog)
    {
        _catalog = customCatalog;
        _catalog?.RemoveNullEntries();
    }

    public static void SetDeduplicationEnabled(bool enabled)
    {
        _enableDeduplication = enabled;
    }

    public static bool IsDeduplicationEnabled()
    {
        return _enableDeduplication;
    }

    private static string ResolveLocalizedText(string fallback, string key)
    {
        if (!string.IsNullOrEmpty(key))
        {
            var resolved = BumiMobile.LocalizationController.Localize(key);
            if (!string.IsNullOrEmpty(resolved) && resolved != key)
            {
                return resolved;
            }
        }

        return fallback ?? string.Empty;
    }

    private static DateTime GetPlayerTime(NotificationScheduleContext context)
    {
        return context?.PlayerLocalTime ?? context?.Now ?? DateTime.Now;
    }

    private static string GenerateIdempotencyKey(NotificationTemplate template, NotificationScheduleContext context, DateTime deliveryDate)
    {
        var playerId = !string.IsNullOrEmpty(context?.PlayerId) ? context.PlayerId : "unknown";
        var type = template.type;
        var trigger = template.trigger.triggerType;
        var entityId = !string.IsNullOrEmpty(template.relatedEntityId) ? template.relatedEntityId : "none";
        var dateKey = deliveryDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        return $"{playerId}:{type}:{trigger}:{dateKey}:{entityId}";
    }

    private static bool IsDuplicateByIdempotency(NotificationTemplate template, NotificationScheduleContext context, DateTime deliveryDate)
    {
        var key = GenerateIdempotencyKey(template, context, deliveryDate);
        return _idempotencyKeys.Contains(key);
    }

    private static void RegisterIdempotencyKey(NotificationTemplate template, NotificationScheduleContext context, DateTime deliveryDate)
    {
        var key = GenerateIdempotencyKey(template, context, deliveryDate);
        _idempotencyKeys.Add(key);
    }

    /// <summary>
    /// Clears pending notifications and schedules the upcoming ones. Called when the app goes to
    /// the background, so every projected notification is cancelled again if the player returns.
    /// </summary>
    public static IReadOnlyList<ScheduledNotification> ScheduleAllNotifications(NotificationScheduleContext context = null)
    {
        context ??= ContextProvider?.Invoke() ?? new NotificationScheduleContext();

        ClearScheduledNotifications();

        var catalog = Catalog;
        if (catalog == null || catalog.Notifications == null || catalog.Notifications.Count == 0)
        {
            Debug.LogWarning("[NotificationManager] No notification templates configured.");
            return Array.Empty<ScheduledNotification>();
        }

        var delivered = NotificationHistoryStore.LoadDelivered(GetPlayerTime(context));
        foreach (var entry in delivered)
        {
            context.AppendHistory(entry.Type, entry.Time);
        }

        var scheduled = BuildSchedule(context, catalog.Notifications);
        foreach (var notification in scheduled)
        {
            SchedulePlatformNotification(notification);
        }

        NotificationHistoryStore.Save(delivered.Concat(scheduled.Select(s => new NotificationHistoryEntry(s.Type, s.FireTime))));

        Debug.Log($"[NotificationManager] Scheduled {scheduled.Count} notification(s): {string.Join(", ", scheduled.Select(s => $"{s.Type}@{s.FireTime:yyyy-MM-dd HH:mm}"))}");
        return scheduled;
    }

    /// <summary>
    /// Resolves which templates fire and when, without touching the platform scheduler.
    /// History in <paramref name="context"/> is treated as already delivered notifications.
    /// </summary>
    public static List<ScheduledNotification> BuildSchedule(NotificationScheduleContext context, IReadOnlyList<NotificationTemplate> templates)
    {
        context ??= new NotificationScheduleContext();
        _idempotencyKeys.Clear();

        // First, collect ALL candidates that pass their template rules and the delivery history.
        var allCandidates = new List<ScheduledNotification>();
        if (templates != null)
        {
            foreach (var template in templates)
            {
                if (template == null)
                {
                    continue;
                }

                foreach (var candidate in DetermineSchedule(template, context))
                {
                    if (!IsBlockedByHistory(candidate, context))
                    {
                        allCandidates.Add(candidate);
                    }
                }
            }
        }

        // Global Rule 1 & 7: max one per day; if multiple qualify on the same day, keep only the highest priority
        allCandidates = FilterByPriority(allCandidates);

        // Global Rule 9 & 16: Copy variation deduplication and merge duplicates
        if (_enableDeduplication)
        {
            allCandidates = DeduplicateNotifications(allCandidates, context);
        }

        // Global Rules 13-18: Idempotency and duplicate prevention
        var scheduled = new List<ScheduledNotification>();
        foreach (var candidate in allCandidates.OrderBy(c => c.FireTime))
        {
            var deliveryDate = candidate.FireTime.Date;
            if (IsDuplicateByIdempotency(candidate.Template, context, deliveryDate))
            {
                Debug.Log($"[NotificationManager] Skipping duplicate notification for {candidate.Type} on {deliveryDate:yyyy-MM-dd}.");
                continue;
            }

            scheduled.Add(candidate);
            RegisterIdempotencyKey(candidate.Template, context, deliveryDate);
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

        var now = GetPlayerTime(context);

        switch (template.type)
        {
            case NotificationType.DailyRetention:
                // Rule: send on H+1 and H+2 after the last play. Reopening the game clears and
                // reschedules, so a player who returns never receives the stale ones.
                foreach (var fireTime in GetFireTimesAfterLastPlay(template.trigger, now, DefaultDailyRetentionDays))
                {
                    yield return CreateSchedule(template, fireTime, false, now);
                }
                break;

            case NotificationType.Reengagement:
                // Rule: Only send to players who have completed at least 1 level
                if (template.trigger.requiresCompletedLevel && context?.HasEverCompletedLevel != true)
                {
                    yield break;
                }

                // Rule: Send on H+3 and H+7 after the last play
                foreach (var fireTime in GetFireTimesAfterLastPlay(template.trigger, now, DefaultReengagementDays))
                {
                    yield return CreateSchedule(template, fireTime, false, now);
                }
                break;

            case NotificationType.MotivateLevelProgression:
                // Rule: Only send if the player has opened the game on that day
                if (context?.LastGameOpenTime == null || context.LastGameOpenTime.Value.Date != now.Date)
                {
                    yield break;
                }

                // Rule: Do not send if the player has completed at least 1 level on that day
                if (context.LevelsCompletedToday > 0)
                {
                    yield break;
                }

                // Rule: Player must have opened the game before the threshold time
                var thresholdTime = new DateTime(now.Year, now.Month, now.Day, template.trigger.gameOpenBeforeHour, template.trigger.gameOpenBeforeMinute, 0);
                if (context.LastGameOpenTime.Value > thresholdTime)
                {
                    yield break;
                }

                // Rule: Same-day push only; the slot has passed if the next one is tomorrow
                var motivateFire = GetNextFixedTime(template.trigger, now);
                if (motivateFire.Date != now.Date)
                {
                    yield break;
                }

                yield return CreateSchedule(template, motivateFire, false, now);
                break;
            case NotificationType.RewardReminder:
                // Rule: Only send if the player has an available reward or bonus that has not been claimed
                if (template.trigger.skipIfDailyRewardClaimed && context?.DailyRewardClaimedToday == true)
                {
                    yield break;
                }

                var hasUnclaimed = context?.UnclaimedRewards != null && context.UnclaimedRewards.Count > 0;
                if (!hasUnclaimed)
                {
                    yield break;
                }

                // Rule: For 12:00 trigger, check 3-day login streak
                if (template.trigger.hour == 12 && context?.LoginStreakDays < template.trigger.loginStreakDays)
                {
                    yield break;
                }

                var rewardFire = GetNextFixedTime(template.trigger, now);
                var rewardSchedule = CreateSchedule(template, rewardFire, false, now);
                yield return rewardSchedule;
                break;

            case NotificationType.FeatureAnnouncement:
                // Rule: Only send to players who completed at least 1 level within the last 7 days
                // (Caller should set up context appropriately; we check if LastGameOpenTime is within 7 days)
                if (context?.LastGameOpenTime == null || (now - context.LastGameOpenTime.Value).TotalDays > 7)
                {
                    yield break;
                }

                // Rule: Do not send if the player has opened the game after the feature release date/time
                if (template.trigger.skipIfFeatureInteracted && context.HasInteractedWithLatestFeature)
                {
                    yield break;
                }

                if (context?.FeatureReleaseDate == null)
                {
                    yield break;
                }

                // Rule: For events, only send while the event is still active
                if (context.CompetitionEndTime.HasValue && now > context.CompetitionEndTime.Value)
                {
                    yield break;
                }

                var featureFire = GetNextFixedTime(template.trigger, now);
                // Ensure it's after the feature release date
                if (featureFire < context.FeatureReleaseDate.Value)
                {
                    featureFire = context.FeatureReleaseDate.Value.AddHours(1);
                }

                var featureSchedule = CreateSchedule(template, featureFire, false, now);
                yield return featureSchedule;
                break;

            case NotificationType.SocialCompetition:
                // Rule: Only send to players who were in the top 100 leaderboard before the reset
                if (context?.LeaderboardRankBeforeReset > 100)
                {
                    yield break;
                }

                // Rule: Only send if the player has not opened the game after the leaderboard reset
                if (context?.LeaderboardResetTime == null)
                {
                    yield break;
                }

                if (context.LastGameOpenTime.HasValue && context.LastGameOpenTime.Value > context.LeaderboardResetTime.Value)
                {
                    yield break;
                }

                var competitionFire = GetNextFixedTime(template.trigger, now);
                var competitionSchedule = CreateSchedule(template, competitionFire, false, now);
                yield return competitionSchedule;
                break;
        }
    }

    private static List<ScheduledNotification> FilterByPriority(List<ScheduledNotification> candidates)
    {
        if (candidates == null || candidates.Count <= 1)
        {
            return candidates ?? new List<ScheduledNotification>();
        }

        // Global Rule 7: If a player qualifies for more than one push trigger on the same day,
        // send only the push with the highest priority.
        var playerDateGroups = candidates.GroupBy(n => n.FireTime.Date);
        var result = new List<ScheduledNotification>();

        foreach (var dateGroup in playerDateGroups)
        {
            var ordered = dateGroup
                .OrderBy(n => (int)n.Template.rules.priority)
                .ThenBy(n => n.FireTime)
                .ToList();
            var highestPriority = ordered.First();
            result.Add(highestPriority);

            if (ordered.Count > 1)
            {
                Debug.Log($"[NotificationManager] Priority filter: {ordered.Count} notifications for {dateGroup.Key:yyyy-MM-dd}. Selected {highestPriority.Type} with priority {(int)highestPriority.Template.rules.priority}. Skipped: {string.Join(", ", ordered.Skip(1).Select(o => o.Type))}.");
            }
        }

        return result;
    }

    private static List<ScheduledNotification> DeduplicateNotifications(
        List<ScheduledNotification> candidates,
        NotificationScheduleContext context)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return candidates;
        }

        var result = new List<ScheduledNotification>();

        // Group by notification type + delivery date (copy variation handling)
        var groups = candidates.GroupBy(n => new { n.Type, Date = n.FireTime.Date });

        foreach (var group in groups)
        {
            var notifications = group.ToList();

            if (notifications.Count > 1)
            {
                // Rule 9: Same type + same trigger on same day = copy variations. Pick one, skip rest.
                // Rule 16: Multiple jobs/segments = merge and send one.
                var selectedIndex = _random.Next(notifications.Count);
                var selected = notifications[selectedIndex];
                result.Add(selected);

                Debug.Log($"[NotificationManager] Copy variation / duplicate merge: {notifications.Count} {group.Key.Type} notifications for {group.Key.Date:yyyy-MM-dd}. Selected index {selectedIndex}. Skipped {notifications.Count - 1}.");
            }
            else
            {
                result.Add(notifications[0]);
            }
        }

        return result;
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

    private static IEnumerable<DateTime> GetFireTimesAfterLastPlay(NotificationTriggerSettings trigger, DateTime lastPlay, int[] fallbackDays)
    {
        var days = trigger.sendOnDaysAfterLastPlay != null && trigger.sendOnDaysAfterLastPlay.Length > 0
            ? trigger.sendOnDaysAfterLastPlay
            : fallbackDays;

        var offset = ParseTimezoneOffset(trigger.timezone);
        var lastPlayInZone = new DateTimeOffset(lastPlay, TimeZoneInfo.Local.GetUtcOffset(lastPlay)).ToOffset(offset);

        foreach (var day in days.Where(d => d > 0).Distinct().OrderBy(d => d))
        {
            var date = lastPlayInZone.Date.AddDays(day);
            var fire = new DateTimeOffset(date.Year, date.Month, date.Day, trigger.hour, trigger.minute, 0, offset);
            yield return fire.ToLocalTime().DateTime;
        }
    }

    private static bool IsBlockedByHistory(ScheduledNotification candidate, NotificationScheduleContext context)
    {
        var fireTime = candidate.FireTime;

        foreach (NotificationType type in Enum.GetValues(typeof(NotificationType)))
        {
            foreach (var sent in context.GetHistory(type))
            {
                // Global Rule 1: max 1 push per player per day
                if (sent.Date == fireTime.Date)
                {
                    return true;
                }

                // Global Rule 2: 4-hour cooldown between any notifications
                if (Math.Abs((fireTime - sent).TotalHours) < GlobalCooldownHours)
                {
                    return true;
                }
            }
        }

        var rules = candidate.Template.rules;
        var ownHistory = context.GetHistory(candidate.Type);

        if (rules.cooldownHours > 0 && ownHistory.Any(t => t <= fireTime && t > fireTime.AddHours(-rules.cooldownHours)))
        {
            return true;
        }

        var window = GetPeriodWindow(rules);
        return rules.maxPerPeriod > 0
            && window > TimeSpan.Zero
            && ownHistory.Count(t => t <= fireTime && t > fireTime - window) >= rules.maxPerPeriod;
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

    private static void SchedulePlatformNotification(ScheduledNotification scheduled)
    {
        if (scheduled == null)
        {
            return;
        }

        var msg = scheduled.Template.message;
        var resolvedTitle = ResolveLocalizedText(msg.title, msg.titleKey);
        var resolvedBody = ResolveLocalizedText(msg.body, msg.bodyKey);
        var resolvedCta = ResolveLocalizedText(msg.callToAction, msg.callToActionKey);

#if UNITY_ANDROID
        var notification = new AndroidNotification
        {
            Title = resolvedTitle,
            Text = resolvedBody,
            FireTime = scheduled.FireTime,
            SmallIcon = !string.IsNullOrEmpty(scheduled.Template.media.iconResource) ? scheduled.Template.media.iconResource : "icon_small"
        };

        if (!string.IsNullOrEmpty(scheduled.Template.media.largePictureResource))
        {
            notification.LargeIcon = scheduled.Template.media.largePictureResource;
        }

        notification.RepeatInterval = scheduled.RepeatDaily ? TimeSpan.FromDays(1) : (TimeSpan?)null;

        if (!string.IsNullOrEmpty(resolvedCta))
        {
            notification.IntentData = resolvedCta;
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
            Title = resolvedTitle,
            Body = resolvedBody,
            ShowInForeground = true,
            ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound,
            Trigger = trigger
        };

        if (!string.IsNullOrEmpty(resolvedCta))
        {
            notification.UserInfo["cta"] = resolvedCta;
        }

        iOSNotificationCenter.ScheduleNotification(notification);
#else
        Debug.Log($"[NotificationManager] Scheduled '{resolvedTitle}' for {scheduled.FireTime} (repeatDaily: {scheduled.RepeatDaily})");
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

[Serializable]
public class UnclaimedReward
{
    public string RewardId;
    public DateTime? ExpirationTime;
    public bool IsDailyReward;
}

public sealed class NotificationScheduleContext
{
    public string PlayerId { get; set; }
    public DateTime Now { get; set; } = DateTime.Now;
    public DateTime? PlayerLocalTime { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime? LastGameOpenTime { get; set; }
    public bool HasEverCompletedLevel { get; set; }
    public int LevelsCompletedToday { get; set; }
    public int LevelsStartedToday { get; set; }
    public int LoginStreakDays { get; set; }
    public List<UnclaimedReward> UnclaimedRewards { get; set; } = new List<UnclaimedReward>();
    public DateTime? LevelStuckSince { get; set; }
    public bool DailyRewardClaimedToday { get; set; }
    public bool HasInteractedWithLatestFeature { get; set; }
    public DateTime? FeatureLaunchTime { get; set; }
    public DateTime? FeatureReleaseDate { get; set; }
    public DateTime? CompetitionStartTime { get; set; }
    public DateTime? CompetitionEndTime { get; set; }
    public int LeaderboardRankBeforeReset { get; set; } = int.MaxValue;
    public DateTime? LeaderboardResetTime { get; set; }

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
