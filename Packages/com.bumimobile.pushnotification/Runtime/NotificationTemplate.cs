using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "NotificationTemplate", menuName = "BumiMobile/Push Notifications/Notification Template")]
public class NotificationTemplate : ScriptableObject
{
#if UNITY_EDITOR
    [SerializeField, HideInInspector]
    private bool hasInitialized;
#endif

    [Tooltip("The notification type associated with this template.")]
    public NotificationType type = NotificationType.DailyRetention;

    [Tooltip("Copy variation ID. If multiple templates share the same type and copyVariationId, only one will be sent per day.")]
    public string copyVariationId;

    [Tooltip("Related entity ID (event ID, feature ID, reward ID, leaderboard reset ID) for idempotency and tracking.")]
    public string relatedEntityId;

    public NotificationRules rules = new NotificationRules();
    public NotificationTriggerSettings trigger = new NotificationTriggerSettings();
    public NotificationMessage message = new NotificationMessage();
    public NotificationMediaAssets media = new NotificationMediaAssets();

    public string DisplayLabel => name;

#if UNITY_EDITOR
    private void OnEnable()
    {
        if (hasInitialized)
        {
            return;
        }

        NotificationTemplateDefaults.ApplyDefaults(this);
        hasInitialized = true;
        EditorUtility.SetDirty(this);
    }

    [ContextMenu("Apply Built-in Defaults")]
    private void ApplyBuiltInDefaults()
    {
        NotificationTemplateDefaults.ApplyDefaults(this, forceRename: true);
        EditorUtility.SetDirty(this);
    }
#endif
}

public enum NotificationType
{
    DailyRetention,
    Reengagement,
    MotivateLevelProgression,
    RewardReminder,
    FeatureAnnouncement,
    SocialCompetition
}

public enum NotificationPriority
{
    FeatureAnnouncement = 1,
    RewardReminder = 2,
    Reengagement = 3,
    SocialCompetition = 4,
    MotivateLevelProgression = 5,
    DailyRetention = 6
}

public enum NotificationPeriod
{
    Day,
    Week,
    Event,
    Custom
}

public enum NotificationTriggerType
{
    DailyAtLocalTime,
    InactivityThreshold,
    LevelStuck,
    FeatureEvent,
    CompetitionLifecycle
}

[Serializable]
public class NotificationRules
{
    public NotificationPriority priority = NotificationPriority.DailyRetention;
    public int maxPerPeriod = 1;
    public NotificationPeriod period = NotificationPeriod.Day;
    public int periodHoursOverride;
    [TextArea]
    public string targetDescription;
    public int cooldownHours;
    [TextArea]
    public string exceptionDescription;
}

[Serializable]
public class NotificationTriggerSettings
{
    public NotificationTriggerType triggerType = NotificationTriggerType.DailyAtLocalTime;

    [Range(0, 23)]
    public int hour = 12;

    [Range(0, 59)]
    public int minute;

    public string timezone = "GMT+8";

    public int inactivityThresholdHours = 72;
    public int levelStuckThresholdHours = 24;
    public int eventDelayMinutes;
    public int competitionLastCallLeadHours = 48;
    public int reengagementBlockHours = 72;

    public bool skipIfLoggedInToday;
    public bool skipIfDailyRewardClaimed;
    public bool skipIfFeatureInteracted;
    public bool includeStartEvent = true;
    public bool includeLastCall = true;

    [Tooltip("Only send to players who have completed at least 1 level.")]
    public bool requiresCompletedLevel;

    [Tooltip("For Motivate Level Progression: player must have opened the game before this hour.")]
    [Range(0, 23)]
    public int gameOpenBeforeHour = 14;

    [Tooltip("For Motivate Level Progression: player must have opened the game before this minute.")]
    [Range(0, 59)]
    public int gameOpenBeforeMinute = 30;

    [Tooltip("For Reward Reminder: required login streak days (e.g. 3 for 3-day streak).")]
    public int loginStreakDays = 3;

    [TextArea]
    public string description;
}

[Serializable]
public class NotificationMessage
{
    public string title;
    [TextArea]
    public string body;
    public string callToAction;

    [Tooltip("Localization key for the title. If set, it will be resolved at runtime. Fallback to 'title' if resolver is not registered.")]
    public string titleKey;

    [Tooltip("Localization key for the body. If set, it will be resolved at runtime. Fallback to 'body' if resolver is not registered.")]
    public string bodyKey;

    [Tooltip("Localization key for the call to action. If set, it will be resolved at runtime. Fallback to 'callToAction' if resolver is not registered.")]
    public string callToActionKey;
}

[Serializable]
public class NotificationMediaAssets
{
    public string largePictureResource;
    public string iconResource;
    public string iconEmoji;
}

public static class NotificationTemplateDefaults
{
    private sealed class TemplateDefinition
    {
        public TemplateDefinition(string label, NotificationType type, Action<NotificationTemplate> configure)
        {
            Label = label;
            Type = type;
            Configure = configure;
        }

        public string Label { get; }
        public NotificationType Type { get; }
        public Action<NotificationTemplate> Configure { get; }
    }

    private static readonly List<TemplateDefinition> Definitions = new List<TemplateDefinition>
    {
        new TemplateDefinition("Daily Retention", NotificationType.DailyRetention, template =>
        {
            template.rules.priority = NotificationPriority.DailyRetention;
            template.rules.maxPerPeriod = 1;
            template.rules.period = NotificationPeriod.Day;
            template.rules.targetDescription = "Players who opened the game before but have not opened since 00:00 today.";
            template.rules.cooldownHours = 4;
            template.rules.exceptionDescription = "Skip if player already logged in today. Skip if player is currently active. Send only on H+1 and H+2.";

            template.trigger.triggerType = NotificationTriggerType.DailyAtLocalTime;
            template.trigger.hour = 10;
            template.trigger.minute = 0;
            template.trigger.timezone = "GMT+8";
            template.trigger.skipIfLoggedInToday = true;
            template.trigger.inactivityThresholdHours = 24;
            template.trigger.description = "Send at 10:00 on H+1 or H+2 after the player last opened the game. Do not send on the same day the player last opened.";

            template.message.title = "We miss you! Come back 🎁";
            template.message.body = "Your daily reward is waiting. Don't break your streak!";
            template.message.callToAction = "Play now and claim your reward!";
            template.message.titleKey = "notification.daily_retention.title";
            template.message.bodyKey = "notification.daily_retention.body";
            template.message.callToActionKey = "notification.daily_retention.cta";
            template.media.iconEmoji = "🎁";
        }),
        new TemplateDefinition("Re-engagement", NotificationType.Reengagement, template =>
        {
            template.rules.priority = NotificationPriority.Reengagement;
            template.rules.maxPerPeriod = 1;
            template.rules.period = NotificationPeriod.Week;
            template.rules.targetDescription = "Players inactive for H+3 or H+7 who have completed at least 1 level.";
            template.rules.cooldownHours = 4;
            template.rules.exceptionDescription = "Skip if player already logged in today. Reset inactive-day count if player opens the game.";

            template.trigger.triggerType = NotificationTriggerType.InactivityThreshold;
            template.trigger.inactivityThresholdHours = 72;
            template.trigger.hour = 10;
            template.trigger.minute = 0;
            template.trigger.timezone = "GMT+8";
            template.trigger.requiresCompletedLevel = true;
            template.trigger.skipIfLoggedInToday = true;
            template.trigger.description = "Send at 10:00 on H+3 (and optionally H+7). Only if player has not opened the game on that day.";

            template.message.title = "We miss you! 🔧";
            template.message.body = "The bolts won't unscrew themselves. Return now and keep progressing!";
            template.message.callToAction = "Come back and continue your puzzle journey!";
            template.message.titleKey = "notification.reengagement.title";
            template.message.bodyKey = "notification.reengagement.body";
            template.message.callToActionKey = "notification.reengagement.cta";
            template.media.iconEmoji = "🔧";
        }),
        new TemplateDefinition("Motivate Level Progression", NotificationType.MotivateLevelProgression, template =>
        {
            template.rules.priority = NotificationPriority.MotivateLevelProgression;
            template.rules.maxPerPeriod = 1;
            template.rules.period = NotificationPeriod.Day;
            template.rules.cooldownHours = 4;
            template.rules.targetDescription = "Players who opened the game today but completed 0 levels.";
            template.rules.exceptionDescription = "Skip if player completed at least 1 level today. Push 1 and Push 2 are copy variations.";

            template.trigger.triggerType = NotificationTriggerType.DailyAtLocalTime;
            template.trigger.hour = 15;
            template.trigger.minute = 0;
            template.trigger.timezone = "GMT+8";
            template.trigger.gameOpenBeforeHour = 14;
            template.trigger.gameOpenBeforeMinute = 30;
            template.trigger.description = "Send at 15:00 if player opened game before 14:30 and completed 0 levels today.";

            template.message.title = "Keep going! You're doing great!";
            template.message.body = "You started strong today. Finish a level and claim your progress!";
            template.message.callToAction = "Play now!";
            template.message.titleKey = "notification.motivate_level.title";
            template.message.bodyKey = "notification.motivate_level.body";
            template.message.callToActionKey = "notification.motivate_level.cta";
            template.media.largePictureResource = "motivate_level_large";
            template.media.iconResource = "motivate_level_icon";
            template.media.iconEmoji = string.Empty;
        }),
        new TemplateDefinition("Reward Reminder", NotificationType.RewardReminder, template =>
        {
            template.rules.priority = NotificationPriority.RewardReminder;
            template.rules.maxPerPeriod = 1;
            template.rules.period = NotificationPeriod.Day;
            template.rules.cooldownHours = 4;
            template.rules.targetDescription = "Players with an available unclaimed reward or bonus.";
            template.rules.exceptionDescription = "Do not send if reward already claimed. Prioritize closest expiration time.";

            template.trigger.triggerType = NotificationTriggerType.DailyAtLocalTime;
            template.trigger.hour = 12;
            template.trigger.minute = 0;
            template.trigger.timezone = "GMT+8";
            template.trigger.skipIfDailyRewardClaimed = true;
            template.trigger.loginStreakDays = 3;
            template.trigger.description = "Send at 12:00 if player has a 3-day login streak and unclaimed Daily Reward. Also send at 20:00 if Daily Reward still unclaimed.";

            template.message.title = "Your reward is waiting! 🤔";
            template.message.body = "Don't let it expire! Claim your reward before it's gone.";
            template.message.callToAction = "Claim your reward before it disappears!";
            template.message.titleKey = "notification.reward_reminder.title";
            template.message.bodyKey = "notification.reward_reminder.body";
            template.message.callToActionKey = "notification.reward_reminder.cta";
            template.media.iconEmoji = "🤔";
        }),
        new TemplateDefinition("Feature Announcement", NotificationType.FeatureAnnouncement, template =>
        {
            template.rules.priority = NotificationPriority.FeatureAnnouncement;
            template.rules.maxPerPeriod = 1;
            template.rules.period = NotificationPeriod.Event;
            template.rules.cooldownHours = 0;
            template.rules.targetDescription = "Players who completed at least 1 level within the last 7 days.";
            template.rules.exceptionDescription = "Do not send if player opened the game after feature release or event start. Only send while event is active.";

            template.trigger.triggerType = NotificationTriggerType.FeatureEvent;
            template.trigger.hour = 11;
            template.trigger.minute = 0;
            template.trigger.timezone = "GMT+8";
            template.trigger.skipIfFeatureInteracted = true;
            template.trigger.description = "Send once in the first 11:00 delivery slot after feature release or event start.";

            template.message.title = "This changes EVERYTHING! 🔥";
            template.message.body = "New feature/event just dropped! Don't get left behind. Check it out now!";
            template.message.callToAction = "Be the first to experience this!";
            template.message.titleKey = "notification.feature_announcement.title";
            template.message.bodyKey = "notification.feature_announcement.body";
            template.message.callToActionKey = "notification.feature_announcement.cta";
            template.media.iconEmoji = "🔥";
        }),
        new TemplateDefinition("Social Competition", NotificationType.SocialCompetition, template =>
        {
            template.rules.priority = NotificationPriority.SocialCompetition;
            template.rules.maxPerPeriod = 1;
            template.rules.period = NotificationPeriod.Event;
            template.rules.cooldownHours = 0;
            template.rules.targetDescription = "Players in the top 100 leaderboard before the reset.";
            template.rules.exceptionDescription = "Only send if player has not opened the game after leaderboard reset.";

            template.trigger.triggerType = NotificationTriggerType.CompetitionLifecycle;
            template.trigger.hour = 10;
            template.trigger.minute = 0;
            template.trigger.timezone = "GMT+8";
            template.trigger.competitionLastCallLeadHours = 48;
            template.trigger.includeStartEvent = true;
            template.trigger.includeLastCall = false;
            template.trigger.description = "Send once in the first 10:00 delivery slot after leaderboard reset. Use pre-reset ranking data.";

            template.message.title = "Someone just DESTROYED your record! 😱";
            template.message.body = "Your leaderboard position is under attack! Jump in and show them who's boss before it's too late!";
            template.message.callToAction = "Crush your rivals now!";
            template.message.titleKey = "notification.social_competition.title";
            template.message.bodyKey = "notification.social_competition.body";
            template.message.callToActionKey = "notification.social_competition.cta";
            template.media.iconEmoji = "😱";
        })
    };

    public static List<NotificationTemplate> CreateRuntimeTemplates()
    {
        var templates = new List<NotificationTemplate>(Definitions.Count);
        foreach (var definition in Definitions)
        {
            var template = ScriptableObject.CreateInstance<NotificationTemplate>();
            template.name = string.IsNullOrWhiteSpace(definition.Label) ? $"{definition.Type}Template" : definition.Label;
            template.type = definition.Type;
            definition.Configure?.Invoke(template);
            templates.Add(template);
        }

        return templates;
    }

    public static void ApplyDefaults(NotificationTemplate template, bool forceRename = false)
    {
        if (template == null)
        {
            return;
        }

        var definition = Definitions.FirstOrDefault(d => d.Type == template.type);
        if (definition == null)
        {
            return;
        }

        definition.Configure?.Invoke(template);

#if UNITY_EDITOR
        if (forceRename)
        {
            template.name = string.IsNullOrWhiteSpace(definition.Label) ? $"{template.type}Template" : definition.Label;
        }
#endif
    }
}
