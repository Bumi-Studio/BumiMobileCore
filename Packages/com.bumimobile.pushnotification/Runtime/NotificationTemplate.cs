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
}

[Serializable]
public class NotificationMediaAssets
{
    public string largePictureResource;
    public string iconResource;
    public string iconEmoji;
}

internal static class NotificationTemplateDefaults
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
            template.rules.maxPerPeriod = 1;
            template.rules.period = NotificationPeriod.Day;
            template.rules.targetDescription = "All active players";
            template.rules.cooldownHours = 24;
            template.rules.exceptionDescription = "Skip if player already logged in today.";

            template.trigger.triggerType = NotificationTriggerType.DailyAtLocalTime;
            template.trigger.hour = 12;
            template.trigger.minute = 0;
            template.trigger.timezone = "GMT+8";
            template.trigger.skipIfLoggedInToday = true;
            template.trigger.description = "Every day at 12:00 PM (GMT+8), if the player hasn't logged in.";

            template.message.title = "Your Daily Gift Awaits 🎁";
            template.message.body = "Don't miss today's reward. Extra coins are waiting for you.";
            template.message.callToAction = "Play now and claim your daily reward!";
            template.media.iconEmoji = "🎁";
        }),
        new TemplateDefinition("Re-engagement", NotificationType.Reengagement, template =>
        {
            template.rules.maxPerPeriod = 2;
            template.rules.period = NotificationPeriod.Week;
            template.rules.targetDescription = "Players inactive for >=72h (since last login)";
            template.rules.cooldownHours = 72;
            template.rules.exceptionDescription = string.Empty;

            template.trigger.triggerType = NotificationTriggerType.InactivityThreshold;
            template.trigger.inactivityThresholdHours = 72;
            template.trigger.hour = 15;
            template.trigger.minute = 0;
            template.trigger.timezone = "GMT+8";
            template.trigger.description = "At 15:00 (GMT+8), if the player's last login was 72h ago or more.";

            template.message.title = "We miss you! 🔧";
            template.message.body = "The bolts won't unscrew themselves, return now and keep progressing.";
            template.message.callToAction = "Come back and continue your puzzle journey!";
            template.media.iconEmoji = "🔧";
        }),
        new TemplateDefinition("Motivate Level Progression", NotificationType.MotivateLevelProgression, template =>
        {
            template.rules.maxPerPeriod = 3;
            template.rules.period = NotificationPeriod.Custom;
            template.rules.periodHoursOverride = 72;
            template.rules.cooldownHours = 8;
            template.rules.targetDescription = "Active players who have logged in for three consecutive days.";
            template.rules.exceptionDescription = "Skip if the player already received a notification in the last 8 hours.";

            template.trigger.triggerType = NotificationTriggerType.DailyAtLocalTime;
            template.trigger.hour = 8;
            template.trigger.minute = 0;
            template.trigger.timezone = "GMT+8";
            template.trigger.description = "Send at 08:00 (GMT+8) after three consecutive days of play, for up to three days.";

            template.message.title = "Capai level tertinggi!";
            template.message.body = "Terus main, capai level tertinggi, dan dapatkan hadiahnya!";
            template.message.callToAction = "Main sekarang!";
            template.media.largePictureResource = "motivate_level_large";
            template.media.iconResource = "motivate_level_icon";
            template.media.iconEmoji = string.Empty;
        }),
        new TemplateDefinition("Reward Reminder", NotificationType.RewardReminder, template =>
        {
            template.rules.maxPerPeriod = 1;
            template.rules.period = NotificationPeriod.Day;
            template.rules.cooldownHours = 24;
            template.rules.targetDescription = "Players who haven't claimed their daily reward or special shop offers.";
            template.rules.exceptionDescription = "Do not send if the daily reward has already been claimed.";

            template.trigger.triggerType = NotificationTriggerType.DailyAtLocalTime;
            template.trigger.hour = 20;
            template.trigger.minute = 0;
            template.trigger.timezone = "GMT+8";
            template.trigger.skipIfDailyRewardClaimed = true;
            template.trigger.description = "Every day at 20:00 (GMT+8), only if the daily reward is still unclaimed.";

            template.message.title = "Why miss free loot? 🤔";
            template.message.body = "Log in now! Grab today's coins before the daily reset wipes them out.";
            template.message.callToAction = "Claim your reward before it disappears!";
            template.media.iconEmoji = "🤔";
        }),
        new TemplateDefinition("Feature Announcement", NotificationType.FeatureAnnouncement, template =>
        {
            template.rules.maxPerPeriod = 1;
            template.rules.period = NotificationPeriod.Event;
            template.rules.cooldownHours = 48;
            template.rules.targetDescription = "All active players (logged in within last 7 days).";
            template.rules.exceptionDescription = "Do not send if the player has already interacted with the new feature.";

            template.trigger.triggerType = NotificationTriggerType.FeatureEvent;
            template.trigger.eventDelayMinutes = 60;
            template.trigger.skipIfFeatureInteracted = true;
            template.trigger.description = "Within 60 minutes after a feature or event launch, send to all players.";

            template.message.title = "This changes EVERYTHING! 🔥";
            template.message.body = "New feature/event just dropped and players are going crazy! Don't get left behind. Check it out now!";
            template.message.callToAction = "Be the first to experience this!";
            template.media.iconEmoji = "🔥";
        }),
        new TemplateDefinition("Social Competition", NotificationType.SocialCompetition, template =>
        {
            template.rules.maxPerPeriod = 2;
            template.rules.period = NotificationPeriod.Event;
            template.rules.cooldownHours = 0;
            template.rules.targetDescription = "All active players (logged in within last 7 days).";
            template.rules.exceptionDescription = string.Empty;

            template.trigger.triggerType = NotificationTriggerType.CompetitionLifecycle;
            template.trigger.competitionLastCallLeadHours = 48;
            template.trigger.includeStartEvent = true;
            template.trigger.includeLastCall = true;
            template.trigger.description = "Send when a competition starts and 48 hours before it ends.";

            template.message.title = "Someone just DESTROYED your record! 😱";
            template.message.body = "Your leaderboard position is under attack! Jump in and show them who's boss before it's too late!";
            template.message.callToAction = "Crush your rivals now!";
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
