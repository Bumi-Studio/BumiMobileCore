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

            return new SettingsProvider(SettingsPath, SettingsScope.Project)
            {
                label = "Build",
                keywords = new[]
                {
                    "Bumi Mobile",
                    "Build",
                    "Android",
                    "Build Profile",
                    "Version",
                    "Keystore"
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
                    EditorGUILayout.HelpBox(
                        "Build type, package format, version, and publishing settings are read from the selected Android Build Profile. Enable Customize Player Settings on each profile to edit version and keystore values in the confirmation window.",
                        MessageType.Info);

                    if (EditorGUI.EndChangeCheck())
                    {
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
