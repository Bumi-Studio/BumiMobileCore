using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NotificationTemplate))]
public class NotificationTemplateEditor : Editor
{
    private SerializedProperty typeProp;
    private SerializedProperty copyVariationIdProp;
    private SerializedProperty relatedEntityIdProp;
    private SerializedProperty rulesProp;
    private SerializedProperty triggerProp;
    private SerializedProperty messageProp;
    private SerializedProperty mediaProp;

    private GUIStyle headerStyle;
    private GUIStyle helpBoxStyle;
    private Color[] typeColors;
    private string[] typeNames;
    private NotificationType _previousType;
    private bool _showAllFields = false;

    private void OnEnable()
    {
        typeProp = serializedObject.FindProperty("type");
        copyVariationIdProp = serializedObject.FindProperty("copyVariationId");
        relatedEntityIdProp = serializedObject.FindProperty("relatedEntityId");
        rulesProp = serializedObject.FindProperty("rules");
        triggerProp = serializedObject.FindProperty("trigger");
        messageProp = serializedObject.FindProperty("message");
        mediaProp = serializedObject.FindProperty("media");

        typeColors = new Color[]
        {
            new Color(0.2f, 0.6f, 1.0f),   // DailyRetention - Blue
            new Color(1.0f, 0.4f, 0.2f),   // Reengagement - Orange
            new Color(0.4f, 0.8f, 0.4f),   // MotivateLevelProgression - Green
            new Color(1.0f, 0.8f, 0.2f),   // RewardReminder - Yellow
            new Color(0.8f, 0.4f, 1.0f),   // FeatureAnnouncement - Purple
            new Color(1.0f, 0.3f, 0.5f),   // SocialCompetition - Pink
        };

        typeNames = new string[]
        {
            "Daily Retention",
            "Re-engagement",
            "Motivate Level Progression",
            "Reward Reminder",
            "Feature Announcement",
            "Social Competition"
        };

        _previousType = (NotificationType)typeProp.enumValueIndex;
        _showAllFields = EditorPrefs.GetBool("NotificationTemplateEditor_ShowAllFields", false);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawTypeHeader();

        // Detect type change and apply defaults
        var currentType = (NotificationType)typeProp.enumValueIndex;
        if (currentType != _previousType)
        {
            _previousType = currentType;

            // Commit the type change to the underlying object first
            // so ApplyDefaults sees the new type.
            serializedObject.ApplyModifiedProperties();

            var template = (NotificationTemplate)target;
            NotificationTemplateDefaults.ApplyDefaults(template);
            EditorUtility.SetDirty(template);

            // Re-serialize to reflect the new defaults in the inspector
            serializedObject.Update();
        }

        DrawTypeHelpBox();
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Advanced", EditorStyles.boldLabel, GUILayout.Width(80));
        var newShowAll = EditorGUILayout.ToggleLeft("Show All Fields", _showAllFields);
        if (newShowAll != _showAllFields)
        {
            _showAllFields = newShowAll;
            EditorPrefs.SetBool("NotificationTemplateEditor_ShowAllFields", _showAllFields);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10);

        DrawIdentitySection();
        EditorGUILayout.Space(10);

        DrawRulesSection();
        EditorGUILayout.Space(10);

        DrawTriggerSection();
        EditorGUILayout.Space(10);

        DrawMessageSection();
        EditorGUILayout.Space(10);

        DrawMediaSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawTypeHeader()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 10, 10)
            };
        }

        int typeIndex = typeProp.enumValueIndex;
        Color typeColor = typeColors[Mathf.Clamp(typeIndex, 0, typeColors.Length - 1)];
        string typeName = typeNames[Mathf.Clamp(typeIndex, 0, typeNames.Length - 1)];

        Rect headerRect = EditorGUILayout.GetControlRect(false, 40);
        EditorGUI.DrawRect(headerRect, typeColor);

        Color previousColor = GUI.color;
        GUI.color = Color.white;
        EditorGUI.LabelField(headerRect, typeName, headerStyle);
        GUI.color = previousColor;

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Notification Type", EditorStyles.boldLabel, GUILayout.Width(120));
        EditorGUILayout.PropertyField(typeProp, GUIContent.none);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTypeHelpBox()
    {
        if (helpBoxStyle == null)
        {
            helpBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10),
                richText = true
            };
        }

        var type = (NotificationType)typeProp.enumValueIndex;
        string helpText = GetHelpTextForType(type);
        EditorGUILayout.LabelField(helpText, helpBoxStyle);
    }

    private void DrawIdentitySection()
    {
        EditorGUILayout.LabelField("Identity & Tracking", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(copyVariationIdProp, new GUIContent("Copy Variation ID", "If multiple templates share the same type and copyVariationId, only one will be sent per day."));
        EditorGUILayout.PropertyField(relatedEntityIdProp, new GUIContent("Related Entity ID", "Event ID, Feature ID, Reward ID, or Leaderboard Reset ID for idempotency."));
    }

    private void DrawRulesSection()
    {
        EditorGUILayout.LabelField("Rules", EditorStyles.boldLabel);

        var priorityProp = rulesProp.FindPropertyRelative("priority");
        var maxPerPeriodProp = rulesProp.FindPropertyRelative("maxPerPeriod");
        var periodProp = rulesProp.FindPropertyRelative("period");
        var periodHoursOverrideProp = rulesProp.FindPropertyRelative("periodHoursOverride");
        var cooldownHoursProp = rulesProp.FindPropertyRelative("cooldownHours");
        var targetDescProp = rulesProp.FindPropertyRelative("targetDescription");
        var exceptionDescProp = rulesProp.FindPropertyRelative("exceptionDescription");

        EditorGUILayout.PropertyField(priorityProp, new GUIContent("Priority", "Higher priority notifications win when multiple qualify on the same day."));
        EditorGUILayout.PropertyField(maxPerPeriodProp, new GUIContent("Max Per Period", "Maximum sends per period for this notification type."));
        EditorGUILayout.PropertyField(periodProp);

        var period = (NotificationPeriod)periodProp.enumValueIndex;
        if (period == NotificationPeriod.Custom)
        {
            EditorGUILayout.PropertyField(periodHoursOverrideProp, new GUIContent("Period Hours Override"));
        }

        EditorGUILayout.PropertyField(cooldownHoursProp, new GUIContent("Cooldown Hours", "Do not send if another notification of this type was sent within this many hours."));
        EditorGUILayout.PropertyField(targetDescProp, new GUIContent("Target Description"));
        EditorGUILayout.PropertyField(exceptionDescProp, new GUIContent("Exception Description"));
    }

    private void DrawTriggerSection()
    {
        EditorGUILayout.LabelField("Trigger Settings", EditorStyles.boldLabel);

        var triggerTypeProp = triggerProp.FindPropertyRelative("triggerType");
        var hourProp = triggerProp.FindPropertyRelative("hour");
        var minuteProp = triggerProp.FindPropertyRelative("minute");
        var timezoneProp = triggerProp.FindPropertyRelative("timezone");
        var inactivityThresholdProp = triggerProp.FindPropertyRelative("inactivityThresholdHours");
        var levelStuckThresholdProp = triggerProp.FindPropertyRelative("levelStuckThresholdHours");
        var eventDelayProp = triggerProp.FindPropertyRelative("eventDelayMinutes");
        var competitionLastCallProp = triggerProp.FindPropertyRelative("competitionLastCallLeadHours");
        var reengagementBlockProp = triggerProp.FindPropertyRelative("reengagementBlockHours");
        var skipIfLoggedInTodayProp = triggerProp.FindPropertyRelative("skipIfLoggedInToday");
        var skipIfDailyRewardClaimedProp = triggerProp.FindPropertyRelative("skipIfDailyRewardClaimed");
        var skipIfFeatureInteractedProp = triggerProp.FindPropertyRelative("skipIfFeatureInteracted");
        var includeStartEventProp = triggerProp.FindPropertyRelative("includeStartEvent");
        var includeLastCallProp = triggerProp.FindPropertyRelative("includeLastCall");
        var requiresCompletedLevelProp = triggerProp.FindPropertyRelative("requiresCompletedLevel");
        var gameOpenBeforeHourProp = triggerProp.FindPropertyRelative("gameOpenBeforeHour");
        var gameOpenBeforeMinuteProp = triggerProp.FindPropertyRelative("gameOpenBeforeMinute");
        var loginStreakDaysProp = triggerProp.FindPropertyRelative("loginStreakDays");
        var descriptionProp = triggerProp.FindPropertyRelative("description");

        var type = (NotificationType)typeProp.enumValueIndex;

        EditorGUILayout.PropertyField(triggerTypeProp);
        EditorGUILayout.Space(5);

        // Delivery Time - shown for all types
        EditorGUILayout.LabelField("Delivery Time", EditorStyles.miniBoldLabel);
        EditorGUILayout.IntSlider(hourProp, 0, 23, new GUIContent("Hour"));
        EditorGUILayout.IntSlider(minuteProp, 0, 59, new GUIContent("Minute"));
        EditorGUILayout.PropertyField(timezoneProp);
        EditorGUILayout.Space(5);

        // Type-specific conditional fields
        if (_showAllFields)
        {
            EditorGUILayout.LabelField("All Trigger Fields (Advanced Mode)", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(skipIfLoggedInTodayProp, new GUIContent("Skip If Logged In Today"));
            EditorGUILayout.PropertyField(skipIfDailyRewardClaimedProp, new GUIContent("Skip If Daily Reward Claimed"));
            EditorGUILayout.PropertyField(skipIfFeatureInteractedProp, new GUIContent("Skip If Feature Interacted"));
            EditorGUILayout.PropertyField(requiresCompletedLevelProp, new GUIContent("Requires Completed Level"));
            EditorGUILayout.PropertyField(inactivityThresholdProp, new GUIContent("Inactivity Threshold (Hours)"));
            EditorGUILayout.PropertyField(levelStuckThresholdProp, new GUIContent("Level Stuck Threshold (Hours)"));
            EditorGUILayout.PropertyField(eventDelayProp, new GUIContent("Event Delay (Minutes)"));
            EditorGUILayout.PropertyField(competitionLastCallProp, new GUIContent("Last Call Lead (Hours)"));
            EditorGUILayout.PropertyField(reengagementBlockProp, new GUIContent("Re-engagement Block (Hours)"));
            EditorGUILayout.PropertyField(includeStartEventProp, new GUIContent("Include Start Event"));
            EditorGUILayout.PropertyField(includeLastCallProp, new GUIContent("Include Last Call"));
            EditorGUILayout.IntSlider(gameOpenBeforeHourProp, 0, 23, new GUIContent("Game Open Before Hour"));
            EditorGUILayout.IntSlider(gameOpenBeforeMinuteProp, 0, 59, new GUIContent("Game Open Before Minute"));
            EditorGUILayout.PropertyField(loginStreakDaysProp, new GUIContent("Login Streak Days"));
        }
        else
        {
            switch (type)
            {
                case NotificationType.DailyRetention:
                    EditorGUILayout.LabelField("Daily Retention Conditions", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(skipIfLoggedInTodayProp, new GUIContent("Skip If Logged In Today"));
                    EditorGUILayout.PropertyField(inactivityThresholdProp, new GUIContent("Inactivity Threshold (Hours)", "Used to calculate H+1 and H+2. Default is 24 hours."));
                    EditorGUILayout.HelpBox("Rules:\n• Max 1 per day\n• Only send on H+1 and H+2 after last game open\n• Do not send on the same day the player last opened\n• If player reaches H+3, move to Re-engagement category", MessageType.Info);
                    break;

                case NotificationType.Reengagement:
                    EditorGUILayout.LabelField("Re-engagement Conditions", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(requiresCompletedLevelProp, new GUIContent("Requires Completed Level", "Only send to players who have completed at least 1 level."));
                    EditorGUILayout.PropertyField(skipIfLoggedInTodayProp, new GUIContent("Skip If Logged In Today"));
                    EditorGUILayout.PropertyField(inactivityThresholdProp, new GUIContent("Inactivity Threshold (Hours)", "H+3 = 72 hours, H+7 = 168 hours."));
                    EditorGUILayout.HelpBox("Rules:\n• Max 1 on H+3\n• Max 1 additional on H+7\n• Reset inactive-day count if player opens the game\n• Only send at 10:00", MessageType.Info);
                    break;

                case NotificationType.MotivateLevelProgression:
                    EditorGUILayout.LabelField("Level Progression Conditions", EditorStyles.miniBoldLabel);
                    EditorGUILayout.IntSlider(gameOpenBeforeHourProp, 0, 23, new GUIContent("Game Open Before Hour"));
                    EditorGUILayout.IntSlider(gameOpenBeforeMinuteProp, 0, 59, new GUIContent("Game Open Before Minute"));
                    EditorGUILayout.HelpBox("Rules:\n• Max 1 per day\n• Only send if player opened game today but completed 0 levels\n• Push 1: opened before time, started 0 levels\n• Push 2: opened before time, started >=1 level, completed 0\n• Only send at 15:00", MessageType.Info);
                    break;

                case NotificationType.RewardReminder:
                    EditorGUILayout.LabelField("Reward Reminder Conditions", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(skipIfDailyRewardClaimedProp, new GUIContent("Skip If Daily Reward Claimed"));
                    EditorGUILayout.PropertyField(loginStreakDaysProp, new GUIContent("Login Streak Days", "For 12:00 trigger: required login streak days (e.g. 3)."));
                    EditorGUILayout.HelpBox("Rules:\n• Max 1 per day\n• Only send if unclaimed reward exists\n• Prioritize reward with closest expiration\n• Trigger 1: 12:00 for 3-day streak + unclaimed Daily Reward\n• Trigger 2: 20:00 if Daily Reward still unclaimed", MessageType.Info);
                    break;

                case NotificationType.FeatureAnnouncement:
                    EditorGUILayout.LabelField("Feature/Event Announcement Conditions", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(skipIfFeatureInteractedProp, new GUIContent("Skip If Feature Interacted"));
                    EditorGUILayout.PropertyField(eventDelayProp, new GUIContent("Event Delay (Minutes)", "Delay after feature release before sending."));
                    EditorGUILayout.HelpBox("Rules:\n• Max 1 per new event/feature\n• Only send to players who completed 1+ level in last 7 days\n• Do not send if player opened game after release/start\n• Only send while event is active\n• Send at 11:00 in first slot after release\n• If feature and event same day, prioritize event", MessageType.Info);
                    break;

                case NotificationType.SocialCompetition:
                    EditorGUILayout.LabelField("Social Competition Conditions", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(competitionLastCallProp, new GUIContent("Last Call Lead (Hours)", "Hours before competition end for last-call notification."));
                    EditorGUILayout.PropertyField(includeStartEventProp, new GUIContent("Include Start Event"));
                    EditorGUILayout.PropertyField(includeLastCallProp, new GUIContent("Include Last Call"));
                    EditorGUILayout.HelpBox("Rules:\n• Max 1 per leaderboard reset\n• Only send to top 100 before reset\n• Do not send if player opened game after reset\n• Use pre-reset ranking data\n• Send at 10:00 in first slot after reset", MessageType.Info);
                    break;
            }
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.PropertyField(descriptionProp, new GUIContent("Trigger Description"));
    }

    private void DrawMessageSection()
    {
        EditorGUILayout.LabelField("Message", EditorStyles.boldLabel);

        var titleProp = messageProp.FindPropertyRelative("title");
        var bodyProp = messageProp.FindPropertyRelative("body");
        var ctaProp = messageProp.FindPropertyRelative("callToAction");
        var titleKeyProp = messageProp.FindPropertyRelative("titleKey");
        var bodyKeyProp = messageProp.FindPropertyRelative("bodyKey");
        var ctaKeyProp = messageProp.FindPropertyRelative("callToActionKey");

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Title", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(titleProp, GUIContent.none);
        EditorGUILayout.PropertyField(titleKeyProp, new GUIContent("Localization Key", "If set, this key will be resolved at runtime. Falls back to the title above."));
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Body", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(bodyProp, GUIContent.none);
        EditorGUILayout.PropertyField(bodyKeyProp, new GUIContent("Localization Key", "If set, this key will be resolved at runtime. Falls back to the body above."));
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Call To Action", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(ctaProp, GUIContent.none);
        EditorGUILayout.PropertyField(ctaKeyProp, new GUIContent("Localization Key", "If set, this key will be resolved at runtime. Falls back to the CTA above."));
        EditorGUILayout.EndVertical();
    }

    private void DrawMediaSection()
    {
        EditorGUILayout.LabelField("Media Assets", EditorStyles.boldLabel);

        var largePictureProp = mediaProp.FindPropertyRelative("largePictureResource");
        var iconResourceProp = mediaProp.FindPropertyRelative("iconResource");
        var iconEmojiProp = mediaProp.FindPropertyRelative("iconEmoji");

        EditorGUILayout.PropertyField(largePictureProp, new GUIContent("Large Picture Resource"));
        EditorGUILayout.PropertyField(iconResourceProp, new GUIContent("Icon Resource"));
        EditorGUILayout.PropertyField(iconEmojiProp, new GUIContent("Icon Emoji"));
    }

    private string GetHelpTextForType(NotificationType type)
    {
        switch (type)
        {
            case NotificationType.DailyRetention:
                return "<b>Daily Retention</b>\n" +
                       "Priority: <b>6 (Lowest)</b>\n" +
                       "Goal: Bring back players who haven't opened the game today.\n" +
                       "Send at <b>10:00</b> on H+1 and H+2. Move to Re-engagement at H+3.";

            case NotificationType.Reengagement:
                return "<b>Re-engagement</b>\n" +
                       "Priority: <b>3</b>\n" +
                       "Goal: Win back players who have been inactive for 3 or 7 days.\n" +
                       "Send at <b>10:00</b> on H+3 and H+7. Requires completed level.";

            case NotificationType.MotivateLevelProgression:
                return "<b>Motivate Level Progression</b>\n" +
                       "Priority: <b>5</b>\n" +
                       "Goal: Nudge players who opened the game but haven't completed any levels.\n" +
                       "Send at <b>15:00</b> if 0 levels completed today.";

            case NotificationType.RewardReminder:
                return "<b>Reward Reminder</b>\n" +
                       "Priority: <b>2</b>\n" +
                       "Goal: Remind players to claim unclaimed rewards before they expire.\n" +
                       "Send at <b>12:00</b> (3-day streak) or <b>20:00</b> (unclaimed daily).";

            case NotificationType.FeatureAnnouncement:
                return "<b>Feature / Event Announcement</b>\n" +
                       "Priority: <b>1 (Highest)</b>\n" +
                       "Goal: Notify players about new features and special events.\n" +
                       "Send at <b>11:00</b> in first slot after release/start.";

            case NotificationType.SocialCompetition:
                return "<b>Social Competition</b>\n" +
                       "Priority: <b>4</b>\n" +
                       "Goal: Ignite competitive spirit using leaderboard rankings.\n" +
                       "Send at <b>10:00</b> after leaderboard reset. Top 100 only.";

            default:
                return "Select a notification type to see relevant rules and fields.";
        }
    }
}
