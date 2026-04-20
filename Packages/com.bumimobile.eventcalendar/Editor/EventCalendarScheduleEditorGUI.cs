using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BumiMobile
{
    internal static class EventCalendarScheduleEditorGUI
    {
        private static readonly Dictionary<string, DateTime> CachedUtcValues = new Dictionary<string, DateTime>(StringComparer.Ordinal);

        public static bool DrawScheduleSection(SerializedProperty scheduleProperty, string title, string infoMessage)
        {
            if (scheduleProperty == null)
                return false;

            EditorGUILayoutCustom.BeginBoxGroup(title);
            bool changed = DrawScheduleFields(scheduleProperty, infoMessage);
            EditorGUILayoutCustom.EndBoxGroup();
            return changed;
        }

        public static bool DrawScheduleFields(SerializedProperty scheduleProperty, string infoMessage)
        {
            if (scheduleProperty == null)
                return false;

            SerializedProperty useScheduleProperty = scheduleProperty.FindPropertyRelative("useSchedule");
            SerializedProperty startUtcProperty = scheduleProperty.FindPropertyRelative("startUtc");
            SerializedProperty endUtcProperty = scheduleProperty.FindPropertyRelative("endUtc");
            if (useScheduleProperty == null || startUtcProperty == null || endUtcProperty == null)
                return false;

            bool changed = false;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(useScheduleProperty, new GUIContent("Use Schedule"));
            if (EditorGUI.EndChangeCheck())
                changed = true;

            if (!useScheduleProperty.boolValue)
            {
                EditorGUILayout.HelpBox("This event will not activate automatically at startup.", MessageType.Info);
                return changed;
            }

            if (!string.IsNullOrWhiteSpace(infoMessage))
                EditorGUILayout.HelpBox(infoMessage, MessageType.Info);

            changed |= DrawUtcField(startUtcProperty, "Start UTC");
            changed |= DrawUtcField(endUtcProperty, "End UTC");
            EditorGUILayout.LabelField($"Stored format: {EventCalendarScheduleUtils.UtcDateTimeFormat}", EditorStyles.miniLabel);

            return changed;
        }

        private static bool DrawUtcField(SerializedProperty utcProperty, string label)
        {
            string propertyKey = BuildPropertyKey(utcProperty);
            bool hasStoredValue = !string.IsNullOrWhiteSpace(utcProperty.stringValue);
            bool hasParsedValue = EventCalendarScheduleUtils.TryParseUtc(utcProperty.stringValue, out DateTime storedUtcValue);

            if (hasParsedValue)
                CachedUtcValues[propertyKey] = storedUtcValue;

            if (!CachedUtcValues.TryGetValue(propertyKey, out DateTime editorUtcValue))
            {
                DateTime nowUtc = DateTime.UtcNow;
                editorUtcValue = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, nowUtc.Hour, nowUtc.Minute, nowUtc.Second, DateTimeKind.Utc);
                CachedUtcValues[propertyKey] = editorUtcValue;
            }

            if (hasParsedValue)
                editorUtcValue = storedUtcValue;

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            int year = editorUtcValue.Year;
            int month = editorUtcValue.Month;
            int day = editorUtcValue.Day;
            int hour = editorUtcValue.Hour;
            int minute = editorUtcValue.Minute;
            int second = editorUtcValue.Second;

            bool changed = false;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Date", EditorStyles.miniLabel, GUILayout.Width(34f));
                year = DrawComponentField("Y", year, 58f);
                month = DrawComponentField("M", month, 42f);
                day = DrawComponentField("D", day, 42f);
                GUILayout.FlexibleSpace();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Time", EditorStyles.miniLabel, GUILayout.Width(34f));
                hour = DrawComponentField("h", hour, 42f);
                minute = DrawComponentField("m", minute, 42f);
                second = DrawComponentField("s", second, 42f);

                if (GUILayout.Button("Now", GUILayout.Width(48f)))
                {
                    DateTime nowUtc = DateTime.UtcNow;
                    year = nowUtc.Year;
                    month = nowUtc.Month;
                    day = nowUtc.Day;
                    hour = nowUtc.Hour;
                    minute = nowUtc.Minute;
                    second = nowUtc.Second;
                    changed = true;
                    GUI.FocusControl(null);
                }

                if (GUILayout.Button("Clear", GUILayout.Width(48f)))
                {
                    if (!string.IsNullOrWhiteSpace(utcProperty.stringValue))
                    {
                        utcProperty.stringValue = string.Empty;
                        changed = true;
                    }

                    CachedUtcValues.Remove(propertyKey);
                    GUI.FocusControl(null);
                }

                GUILayout.FlexibleSpace();
            }

            DateTime normalizedUtc = BuildUtcValue(year, month, day, hour, minute, second);
            CachedUtcValues[propertyKey] = normalizedUtc;

            string formattedUtc = EventCalendarScheduleUtils.FormatUtc(normalizedUtc);
            if ((year != editorUtcValue.Year || month != editorUtcValue.Month || day != editorUtcValue.Day ||
                 hour != editorUtcValue.Hour || minute != editorUtcValue.Minute || second != editorUtcValue.Second) &&
                utcProperty.stringValue != formattedUtc)
            {
                utcProperty.stringValue = formattedUtc;
                changed = true;
            }

            EditorGUILayout.LabelField($"Stored: {(string.IsNullOrWhiteSpace(utcProperty.stringValue) ? "<empty>" : utcProperty.stringValue)}", EditorStyles.miniLabel);
            if (hasStoredValue && !hasParsedValue)
                EditorGUILayout.HelpBox("Stored UTC value is invalid. Adjust the fields above or clear it.", MessageType.Warning);

            return changed;
        }

        private static int DrawComponentField(string suffix, int value, float width)
        {
            int nextValue = EditorGUILayout.DelayedIntField(value, GUILayout.Width(width));
            EditorGUILayout.LabelField(suffix, EditorStyles.miniLabel, GUILayout.Width(12f));
            return nextValue;
        }

        private static DateTime BuildUtcValue(int year, int month, int day, int hour, int minute, int second)
        {
            year = Mathf.Clamp(year, 1, 9999);
            month = Mathf.Clamp(month, 1, 12);
            int maxDay = DateTime.DaysInMonth(year, month);
            day = Mathf.Clamp(day, 1, maxDay);
            hour = Mathf.Clamp(hour, 0, 23);
            minute = Mathf.Clamp(minute, 0, 59);
            second = Mathf.Clamp(second, 0, 59);

            return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
        }

        private static string BuildPropertyKey(SerializedProperty property)
        {
            return $"{property.serializedObject.targetObject.GetInstanceID()}::{property.propertyPath}";
        }
    }
}
