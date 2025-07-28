using System.IO;
using UnityEditor;
using UnityEngine;

namespace BumiMobile
{
    [InitializeOnLoad]
    public static class CoreEditor
    {
        // Folders
        public static string FOLDER_CORE { get; private set; }

        public static string FOLDER_CORE_MODULES => Path.Combine(FOLDER_CORE, "Modules");

        public static string FOLDER_DATA;
        public static string FOLDER_SCENES;

        // Editor values
        public static bool UseCustomInspector { get; private set; } = true;
        public static bool UseHierarchyIcons { get; private set; } = true;

        public static bool AutoLoadInitializer { get; private set; } = true;
        public static string InitSceneName { get; private set; } = "Init";

        public static Color AdsDummyBackgroundColor { get; private set; } = new Color(0.2f, 0.2f, 0.3f);
        public static Color AdsDummyMainColor { get; private set; } = new Color(0.2f, 0.3f, 0.7f);

        public static bool ShowWatermelonPromotions { get; private set; } = true;

        static CoreEditor()
        {
            Init();
        }

        private static void Init()
        {
            CoreSettings coreSettings = EditorUtils.GetAsset<CoreSettings>();
            if (coreSettings == null)
            {
                if (EditorApplication.isUpdating || EditorApplication.isCompiling)
                {
                    EditorApplication.delayCall += Init;

                    return;
                }

                Debug.LogWarning("[Bumi Mobilr Core]: Core Settings asset cannot be found in the project. This asset is required for the proper functionality of the modules.");

                coreSettings = ScriptableObject.CreateInstance<CoreSettings>();

                FOLDER_CORE = Path.Combine("Assets", "Bumi Mobile Core");

                if (!AssetDatabase.IsValidFolder(FOLDER_CORE))
                {
                    AssetDatabase.CreateFolder("Assets/", "Bumi Mobile Core");
                }

                AssetDatabase.CreateAsset(coreSettings, Path.Combine("Assets", "Bumi Mobile Core", "Core Settings.asset"));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else
            {
                FOLDER_CORE = AssetDatabase.GetAssetPath(coreSettings).Replace(coreSettings.name + ".asset", "");
            }

            ApplySettings(coreSettings);
        }

        public static void ApplySettings(CoreSettings settings)
        {
            // Folders
            FOLDER_DATA = settings.DataFolder;
            FOLDER_SCENES = settings.ScenesFolder;

            // Init
            InitSceneName = settings.InitSceneName;
            AutoLoadInitializer = settings.AutoLoadInitializer;

            // Editor
            UseCustomInspector = settings.UseCustomInspector;
            UseHierarchyIcons = settings.UseHierarchyIcons;

            // Ads
            AdsDummyBackgroundColor = settings.AdsDummyBackgroundColor;
            AdsDummyMainColor = settings.AdsDummyMainColor;

        }

        public static string FormatPath(string path)
        {
            return path.Replace("{CORE_MODULES}", FOLDER_CORE_MODULES)
                       .Replace("{CORE_DATA}", FOLDER_DATA)
                       .Replace("{CORE}", FOLDER_CORE);
        }
    }
}