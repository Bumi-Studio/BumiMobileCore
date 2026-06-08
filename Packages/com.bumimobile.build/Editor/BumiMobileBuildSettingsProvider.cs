using UnityEditor;
using UnityEngine;

namespace BumiMobile.Build
{
    internal static class BumiMobileBuildSettingsProvider
    {
        private const string SettingsPath = "Project/Bumi Mobile/Build";

        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            BumiMobileBuildSettings settings = BumiMobileBuildSettings.instance;
            settings.EnsureSaved();

            var serializedSettings = new SerializedObject(settings);
            SerializedProperty enableProperty = serializedSettings.FindProperty("enablePreBuildWindow");
            SerializedProperty internalTestLabelProperty = serializedSettings.FindProperty("internalTestLabel");
            SerializedProperty cheatLabelProperty = serializedSettings.FindProperty("cheatLabel");
            SerializedProperty productionLabelProperty = serializedSettings.FindProperty("productionLabel");
            SerializedProperty cheatDefineProperty = serializedSettings.FindProperty("cheatScriptingDefine");

            return new SettingsProvider(SettingsPath, SettingsScope.Project)
            {
                label = "Build",
                keywords = new[]
                {
                    "Bumi Mobile",
                    "Build",
                    "Android",
                    "Version",
                    "Keystore",
                    "Cheat",
                    "Scripting Define"
                },
                guiHandler = _ =>
                {
                    serializedSettings.Update();

                    EditorGUILayout.LabelField("Android Pre-Build Window", EditorStyles.boldLabel);
                    EditorGUILayout.Space(2f);

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(enableProperty, new GUIContent(
                        "Enable Pre-Build Window",
                        "Show the confirmation window before Android builds started from Unity's Build Player window."));

                    EditorGUILayout.Space(8f);
                    EditorGUILayout.LabelField("Preset Labels", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(internalTestLabelProperty, new GUIContent("Internal Test Label"));
                    EditorGUILayout.PropertyField(cheatLabelProperty, new GUIContent("Cheat Label"));
                    EditorGUILayout.PropertyField(productionLabelProperty, new GUIContent("Production Label"));

                    EditorGUILayout.Space(8f);
                    EditorGUILayout.LabelField("Cheat Build", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(cheatDefineProperty, new GUIContent(
                        "Cheat Scripting Define",
                        "Added only to Development - Cheat builds and removed from the other presets."));

                    string cheatDefine = cheatDefineProperty.stringValue == null
                        ? string.Empty
                        : cheatDefineProperty.stringValue.Trim();

                    if (!BumiMobileBuildSettings.IsValidScriptingDefine(cheatDefine))
                    {
                        EditorGUILayout.HelpBox(
                            "Cheat Scripting Define must be a valid C# preprocessor identifier.",
                            MessageType.Error);
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        cheatDefineProperty.stringValue = cheatDefine;
                        serializedSettings.ApplyModifiedProperties();
                        settings.SaveSettings();
                    }

                    EditorGUILayout.Space(12f);
                    if (GUILayout.Button("Reset to Defaults", GUILayout.Width(140f)))
                    {
                        Undo.RecordObject(settings, "Reset Bumi Mobile Build Settings");
                        settings.ResetToDefaults();
                        EditorUtility.SetDirty(settings);
                        settings.SaveSettings();
                        serializedSettings.Update();
                    }
                }
            };
        }
    }
}
