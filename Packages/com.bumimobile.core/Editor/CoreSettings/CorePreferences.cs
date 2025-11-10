using UnityEditor;
using UnityEngine;

namespace BumiMobile
{
    public static class CorePreferences
    {
        [SettingsProvider]
        public static SettingsProvider CustomPreferencesMenu()
        {
            CoreSettings coreSettings = EditorUtils.GetAsset<CoreSettings>();

            Editor editor = null;
            if (coreSettings != null)
            {
                editor = Editor.CreateEditor(coreSettings);
            }

            SettingsProvider provider = new SettingsProvider("Preferences/Bumi Mobile Core", SettingsScope.User)
            {
                label = "Bumi Mobile Core",
                guiHandler = _ =>
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    EditorGUILayout.BeginVertical();

                    if (editor != null)
                    {
                        editor.OnInspectorGUI();
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Core Settings file can't be found!");
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                },
                keywords = new[] { "Custom", "Preferences", "Bumi Mobile Core", "Core" }
            };

            return provider;
        }
    }
}
