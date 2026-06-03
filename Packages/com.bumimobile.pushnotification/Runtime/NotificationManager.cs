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
    private const int SameTimeThresholdMinutes = 5;
    private const int MinimumIntervalHours = 4;
    private const int GlobalMaxPerDay = 1;
    private const int GlobalCooldownHours = 4;

    private static NotificationTemplateCatalog _catalog;
    private static bool _enableDeduplication = true;
    private static System.Random _random = new System.Random();
    private static readonly HashSet<string> _idempotencyKeys = new HashSet<string>();

    public static NotificationTemplateCatalog Catalog => LoadCatalog();

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

    private static DateTime GetPlayerDate(NotificationScheduleContext context)
    {
        var time = GetPlayerTime(context);
        return new DateTime(time.Year, time.Month, time.Day, 0, 0, 0, time.Kind);
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

    private static bool HasAnyNotificationToday(NotificationScheduleContext context)
    {
        if (context == null)
        {
            return false;
        }

        var today = GetPlayerDate(context);
        foreach (var type in Enum.GetValues(typeof(NotificationType)))
        {
            var history = context.GetHistory((NotificationType)type);
            if (history != null && history.Any(h => h.Date == today.Date))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInGlobalCooldown(NotificationScheduleContext context, DateTime now)
    {
        if (context == null)
        {
            return false;
        }

        var threshold = now.AddHours(-GlobalCooldownHours);
        foreach (var type in Enum.GetValues(typeof(NotificationType)))
        {
            var history = context.GetHistory((NotificationType)type);
            if (history != null && history.Any(h => h >= threshold))
            {
                return true;
            }
        }

        return false;
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

    public static IReadOnlyList<ScheduledNotification> ScheduleAllNotifications(NotificationScheduleContext context = null)
    {
        context ??= new NotificationScheduleContext();
        _idempotencyKeys.Clear();

        ClearScheduledNotifications();

        var catalog = Catalog;
        if (catalog == null || catalog.Notifications == null || catalog.Notifications.Count == 0)
        {
            Debug.LogWarning("[NotificationManager] No notification templates configured.");
            return Array.Empty<ScheduledNotification>();
        }

        var now = GetPlayerTime(context);

        // Global Rule 3: Do not send if player is currently active
        if (context.IsPlayerActive)
        {
            Debug.Log("[NotificationManager] Player is currently active. Skipping all notifications.");
            return Array.Empty<ScheduledNotification>();
        }

        // Global Rule 1 & 2: Max 1 per day, 4h cooldown
        if (HasAnyNotificationToday(context))
        {
            Debug.Log("[NotificationManager] Player already received a notification today. Skipping all.");
            return Array.Empty<ScheduledNotification>();
        }

        if (IsInGlobalCooldown(context, now))
        {
            Debug.Log("[NotificationManager] Player is in global cooldown (4h). Skipping all.");
            return Array.Empty<ScheduledNotification>();
        }

        // First, collect ALL candidates
        var allCandidates = new List<ScheduledNotification>();
        foreach (var template in catalog.Notifications)
        {
            if (template == null)
            {
                continue;
            }
            foreach (var candidate in DetermineSchedule(template, context))
            {
                allCandidates.Add(candidate);
            }
        }

        // Global Rule 7: Priority filtering - if multiple qualify on same day, keep only highest priority
        allCandidates = FilterByPriority(allCandidates);

        // Global Rule 9 & 16: Copy variation deduplication and merge duplicates
        if (_enableDeduplication)
        {
            allCandidates = DeduplicateNotifications(allCandidates, context);
        }

        // Global Rules 13-18: Idempotency and duplicate prevention
        var scheduled = new List<ScheduledNotification>();
        foreach (var candidate in allCandidates)
        {
            var deliveryDate = new DateTime(candidate.FireTime.Year, candidate.FireTime.Month, candidate.FireTime.Day);
            if (IsDuplicateByIdempotency(candidate.Template, context, deliveryDate))
            {
                Debug.Log($"[NotificationManager] Skipping duplicate notification for {candidate.Type} on {deliveryDate:yyyy-MM-dd}.");
                continue;
            }

            SchedulePlatformNotification(candidate);
            scheduled.Add(candidate);
            context.AppendHistory(candidate.Type, candidate.FireTime);
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

        switch (template.type)
        {
            case NotificationType.DailyRetention:
                // Rule: Only send to players who have opened the game before
                if (context?.LastGameOpenTime == null)
                {
                    yield break;
                }

                // Rule: Only send if player has not opened the game since 00:00 today
                if (template.trigger.skipIfLoggedInToday && context.HasLoggedInToday)
                {
                    yield break;
                }

                // Rule: Send only on H+1 and H+2 after last open
                var daysSinceLastOpen = (now.Date - context.LastGameOpenTime.Value.Date).Days;
                if (daysSinceLastOpen != 1 && daysSinceLastOpen != 2)
                {
                    yield break;
                }

                // Rule: Do not send on the same day the player last opened the game
                if (now.Date == context.LastGameOpenTime.Value.Date)
                {
                    yield break;
                }

                // Rule: If player still has not opened by H+3, move to Re-engagement (don't send DailyRetention)
                if (daysSinceLastOpen >= 3)
                {
                    yield break;
                }

                if (!HasCapacity())
                {
                    yield break;
                }

                var dailyFireTime = GetNextFixedTime(template.trigger, now);
                var dailySchedule = CreateSchedule(template, dailyFireTime, false, now);
                scheduledCount++;
                yield return dailySchedule;
                break;

            case NotificationType.Reengagement:
                // Rule: Only send to players who have completed at least 1 level
                if (template.trigger.requiresCompletedLevel && context?.HasEverCompletedLevel != true)
                {
                    yield break;
                }

                if (context?.LastGameOpenTime == null)
                {
                    yield break;
                }

                // Rule: Do not send if the player has already opened the game on that day
                if (template.trigger.skipIfLoggedInToday && context.HasLoggedInToday)
                {
                    yield break;
                }

                // Rule: Send on H+3 and H+7
                var inactiveDays = (now.Date - context.LastGameOpenTime.Value.Date).Days;
                if (inactiveDays != 3 && inactiveDays != 7)
                {
                    yield break;
                }

                if (!HasCapacity())
                {
                    yield break;
                }

                var reengagementFire = GetNextFixedTime(template.trigger, now);
                var reengagementSchedule = CreateSchedule(template, reengagementFire, false, now);
                scheduledCount++;
                yield return reengagementSchedule;
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

                if (!HasCapacity())
                {
                    yield break;
                }

                var motivateFire = GetNextFixedTime(template.trigger, now);
                var motivateSchedule = CreateSchedule(template, motivateFire, false, now);
                scheduledCount++;
                yield return motivateSchedule;
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

                if (!HasCapacity())
                {
                    yield break;
                }

                var rewardFire = GetNextFixedTime(template.trigger, now);
                var rewardSchedule = CreateSchedule(template, rewardFire, false, now);
                scheduledCount++;
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

                if (!HasCapacity())
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
                scheduledCount++;
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

                if (!HasCapacity())
                {
                    yield break;
                }

                var competitionFire = GetNextFixedTime(template.trigger, now);
                var competitionSchedule = CreateSchedule(template, competitionFire, false, now);
                scheduledCount++;
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
            notification.UserInfo = new Dictionary<string, string>
            {
                { "cta", resolvedCta }
            };
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
    public bool HasLoggedInToday { get; set; }
    public DateTime? LastGameOpenTime { get; set; }
    public bool IsPlayerActive { get; set; }
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