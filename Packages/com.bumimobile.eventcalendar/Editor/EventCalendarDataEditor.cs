using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BumiMobile
{
    [CustomEditor(typeof(EventCalendarData))]
    public class EventCalendarDataEditor : Editor
    {
        private SerializedProperty eventIdProperty;
        private SerializedProperty displayNameProperty;
        private SerializedProperty spriteLibraryProperty;
        private SerializedProperty scheduleProperty;

        private void OnEnable()
        {
            eventIdProperty = serializedObject.FindProperty("eventId");
            displayNameProperty = serializedObject.FindProperty("displayName");
            spriteLibraryProperty = serializedObject.FindProperty("spriteLibrary");
            scheduleProperty = serializedObject.FindProperty("schedule");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScriptField();
            DrawEventDetails();
            DrawSchedule();
            DrawValidation();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawScriptField()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Script", MonoScript.FromScriptableObject((ScriptableObject)target), typeof(MonoScript), false);
            }
        }

        private void DrawEventDetails()
        {
            EditorGUILayoutCustom.BeginBoxGroup("Event Details");
            EditorGUILayout.HelpBox("Primary authoring lives in EventCalendarDatabase. This inspector is kept for direct edits and validation.", MessageType.Info);
            EditorGUILayout.PropertyField(displayNameProperty);
            EditorGUILayout.PropertyField(eventIdProperty);
            EditorGUILayout.PropertyField(spriteLibraryProperty, new GUIContent("Sprite Library"));
            EditorGUILayout.HelpBox("Assign a local EventSpriteLibrary asset.", MessageType.Info);
            EditorGUILayoutCustom.EndBoxGroup();
        }

        private void DrawSchedule()
        {
            EventCalendarScheduleEditorGUI.DrawScheduleSection(
                scheduleProperty,
                "Local Schedule",
                "Init resolves the active event from local UTC time and applies matching sprites at startup.");
        }

        private void DrawValidation()
        {
            List<string> warnings = new List<string>();

            if (string.IsNullOrWhiteSpace(eventIdProperty.stringValue))
            {
                warnings.Add("Event Id should not be empty.");
            }
            else if (!EventCalendarEditorUtils.TryValidateEventId(eventIdProperty.stringValue, null, out string eventIdError))
            {
                warnings.Add(eventIdError);
            }

            if (spriteLibraryProperty.objectReferenceValue == null)
                warnings.Add("Sprite Library is empty.");

            List<string> scheduleWarnings = EventCalendarScheduleUtils.CollectScheduleWarnings((EventCalendarData)target);
            for (int i = 0; i < scheduleWarnings.Count; i++)
                warnings.Add(scheduleWarnings[i]);

            if (warnings.Count == 0)
                return;

            EditorGUILayoutCustom.BeginBoxGroup("Validation");
            for (int i = 0; i < warnings.Count; i++)
            {
                EditorGUILayout.HelpBox(warnings[i], MessageType.Warning);
            }
            EditorGUILayoutCustom.EndBoxGroup();
        }
    }
}
