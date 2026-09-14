using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BumiMobile
{
    [CustomEditor(typeof(EventSpriteSwapController))]
    public class EventSpriteSwapControllerEditor : Editor
    {
        private SerializedProperty updateModeProperty;
        private SerializedProperty targetsProperty;

        private void OnEnable()
        {
            updateModeProperty = serializedObject.FindProperty("updateMode");
            targetsProperty = serializedObject.FindProperty("targets");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawScriptField();
            DrawTargets();
            DrawValidation();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawScriptField()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), typeof(MonoScript), false);
            }
        }

        private void DrawTargets()
        {
            EventSpriteSwapController controller = (EventSpriteSwapController)target;

            EditorGUILayoutCustom.BeginBoxGroup("Sprite Swap Targets");
            EditorGUILayout.HelpBox("This controller registers itself to EventCalendarService when enabled.", MessageType.Info);
            EditorGUILayout.PropertyField(updateModeProperty);

            if ((EventSpriteSwapUpdateMode)updateModeProperty.enumValueIndex == EventSpriteSwapUpdateMode.FreezeAfterStartupVisual)
            {
                EditorGUILayout.HelpBox("Use this when the first applied event visual should stay locked for the lifetime of this controller.", MessageType.None);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Capture All Defaults"))
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(controller, "Capture Event Sprite Defaults");
                controller.CaptureDefaultSprites();
                EditorUtility.SetDirty(controller);
                serializedObject.Update();
            }

            if (GUILayout.Button("Reset To Defaults"))
            {
                serializedObject.ApplyModifiedProperties();
                controller.ResetToDefault();
            }
            EditorGUILayout.EndHorizontal();

            if (targetsProperty.arraySize == 0)
                EditorGUILayout.HelpBox("No sprite targets registered.", MessageType.Info);

            for (int i = 0; i < targetsProperty.arraySize; i++)
            {
                SerializedProperty element = targetsProperty.GetArrayElementAtIndex(i);
                SerializedProperty targetId = element.FindPropertyRelative("targetId");
                SerializedProperty imageTarget = element.FindPropertyRelative("imageTarget");
                SerializedProperty spriteRendererTarget = element.FindPropertyRelative("spriteRendererTarget");
                SerializedProperty defaultSprite = element.FindPropertyRelative("defaultSprite");
                SerializedProperty hideWhenInactive = element.FindPropertyRelative("hideWhenInactive");
                SerializedProperty useNativeSizeForEventSprite = element.FindPropertyRelative("useNativeSizeForEventSprite");
                SerializedProperty useNativeSizeForEventSpriteInitialized = element.FindPropertyRelative("useNativeSizeForEventSpriteInitialized");
                SerializedProperty defaultActiveState = element.FindPropertyRelative("defaultActiveState");
                SerializedProperty defaultStateCaptured = element.FindPropertyRelative("defaultStateCaptured");
                SerializedProperty defaultImageSize = element.FindPropertyRelative("defaultImageSize");
                SerializedProperty defaultImageSizeCaptured = element.FindPropertyRelative("defaultImageSizeCaptured");

                if (!useNativeSizeForEventSpriteInitialized.boolValue)
                {
                    useNativeSizeForEventSprite.boolValue = true;
                    useNativeSizeForEventSpriteInitialized.boolValue = true;
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Target {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("Capture", GUILayout.Width(70f)))
                {
                    serializedObject.ApplyModifiedProperties();
                    Undo.RecordObject(controller, "Capture Event Sprite Default");
                    controller.CaptureDefaultSpriteAt(i);
                    EditorUtility.SetDirty(controller);
                    serializedObject.Update();
                }

                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    targetsProperty.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(targetId);
                EditorGUILayout.PropertyField(imageTarget);
                EditorGUILayout.PropertyField(spriteRendererTarget);
                EditorGUILayout.PropertyField(hideWhenInactive);
                EditorGUILayout.PropertyField(useNativeSizeForEventSprite, new GUIContent("Use Native Size For Event Sprite"));

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(defaultSprite);
                    EditorGUILayout.PropertyField(defaultImageSize);
                    EditorGUILayout.PropertyField(defaultActiveState);
                    EditorGUILayout.PropertyField(defaultImageSizeCaptured);
                    EditorGUILayout.PropertyField(defaultStateCaptured);
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Target"))
            {
                targetsProperty.arraySize += 1;

                SerializedProperty newElement = targetsProperty.GetArrayElementAtIndex(targetsProperty.arraySize - 1);
                newElement.FindPropertyRelative("useNativeSizeForEventSprite").boolValue = true;
                newElement.FindPropertyRelative("useNativeSizeForEventSpriteInitialized").boolValue = true;
            }

            EditorGUILayoutCustom.EndBoxGroup();
        }

        private void DrawValidation()
        {
            List<string> warnings = new List<string>();
            HashSet<string> targetIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < targetsProperty.arraySize; i++)
            {
                SerializedProperty element = targetsProperty.GetArrayElementAtIndex(i);
                string targetId = element.FindPropertyRelative("targetId").stringValue;
                UnityEngine.Object imageTarget = element.FindPropertyRelative("imageTarget").objectReferenceValue;
                UnityEngine.Object spriteRendererTarget = element.FindPropertyRelative("spriteRendererTarget").objectReferenceValue;
                bool useNativeSizeForEventSprite = element.FindPropertyRelative("useNativeSizeForEventSprite").boolValue;

                if (string.IsNullOrWhiteSpace(targetId))
                    warnings.Add($"Target {i + 1} has an empty Target Id.");
                else if (!targetIds.Add(targetId))
                    warnings.Add($"Duplicate Target Id '{targetId}'.");

                if (imageTarget == null && spriteRendererTarget == null)
                    warnings.Add($"Target {i + 1} does not have an Image or SpriteRenderer assigned.");

                if (imageTarget != null && spriteRendererTarget != null)
                    warnings.Add($"Target {i + 1} should use either Image or SpriteRenderer, not both.");

                if (useNativeSizeForEventSprite && imageTarget == null)
                    warnings.Add($"Target {i + 1} enables native size, but does not have an Image assigned.");
            }

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
