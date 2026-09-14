using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BumiMobile
{
    internal static class EventSpriteLibraryEditorGUI
    {
        private const string SessionKeyPrefix = "BumiMobile.EventSpriteLibraryEditorGUI";

        public static bool DrawLibraryInspector(EventSpriteLibrary library, bool showAuthoringHint, string title = "Sprite Library")
        {
            if (library == null)
                return false;

            string assetPath = AssetDatabase.GetAssetPath(library);
            string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            string sessionKey = string.IsNullOrWhiteSpace(assetGuid)
                ? $"{SessionKeyPrefix}.{library.GetInstanceID()}"
                : $"{SessionKeyPrefix}.{assetGuid}";
            bool isExpanded = SessionState.GetBool(sessionKey, showAuthoringHint);

            SerializedObject serializedObject = new SerializedObject(library);
            SerializedProperty entriesProperty = serializedObject.FindProperty("entries");

            serializedObject.Update();
            bool changed = false;

            isExpanded = EditorGUILayoutCustom.BeginExpandBoxGroup($"{title} ({entriesProperty.arraySize})", isExpanded);
            SessionState.SetBool(sessionKey, isExpanded);

            if (!isExpanded)
            {
                EditorGUILayoutCustom.EndBoxGroup();
                return false;
            }

            if (showAuthoringHint)
                EditorGUILayout.HelpBox("Primary authoring lives in EventCalendarDatabase. This inspector is available for direct edits and validation.", MessageType.Info);

            if (entriesProperty.arraySize == 0)
                EditorGUILayout.HelpBox("No sprite entries added.", MessageType.Info);

            for (int i = 0; i < entriesProperty.arraySize; i++)
            {
                SerializedProperty element = entriesProperty.GetArrayElementAtIndex(i);
                SerializedProperty targetIdProperty = element.FindPropertyRelative("targetId");
                SerializedProperty spriteProperty = element.FindPropertyRelative("sprite");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Entry {i + 1}", EditorStyles.boldLabel);

                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    entriesProperty.DeleteArrayElementAtIndex(i);
                    changed = true;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();
                targetIdProperty.stringValue = EditorGUILayout.DelayedTextField("Target Id", targetIdProperty.stringValue);
                EditorGUILayout.PropertyField(spriteProperty, new GUIContent("Sprite"));
                if (EditorGUI.EndChangeCheck())
                    changed = true;

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Entry"))
            {
                entriesProperty.arraySize += 1;
                changed = true;
            }

            serializedObject.ApplyModifiedProperties();

            if (changed)
                EditorUtility.SetDirty(library);

            DrawValidation(library);

            EditorGUILayoutCustom.EndBoxGroup();

            return changed;
        }

        public static void DrawValidation(EventSpriteLibrary library)
        {
            if (library == null)
                return;

            List<string> warnings = CollectValidationWarnings(library);
            if (warnings.Count == 0)
                return;

            EditorGUILayoutCustom.BeginBoxGroup("Validation");
            for (int i = 0; i < warnings.Count; i++)
            {
                EditorGUILayout.HelpBox(warnings[i], MessageType.Warning);
            }
            EditorGUILayoutCustom.EndBoxGroup();
        }

        public static List<string> CollectValidationWarnings(EventSpriteLibrary library)
        {
            List<string> warnings = new List<string>();
            if (library == null)
                return warnings;

            HashSet<string> targetIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            EventSpriteLibraryEntry[] entries = library.Entries;
            for (int i = 0; i < entries.Length; i++)
            {
                EventSpriteLibraryEntry entry = entries[i];
                if (entry == null)
                {
                    warnings.Add($"Entry {i + 1} is missing.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.TargetId))
                    warnings.Add($"Entry {i + 1} has an empty Target Id.");
                else if (!targetIds.Add(entry.TargetId))
                    warnings.Add($"Duplicate Target Id '{entry.TargetId}'.");

                if (entry.Sprite == null)
                    warnings.Add($"Entry {i + 1} does not have a Sprite assigned.");
            }

            return warnings;
        }
    }
}
