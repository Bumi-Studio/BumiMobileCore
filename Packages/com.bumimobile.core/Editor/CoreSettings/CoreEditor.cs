using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BumiMobile
{
    [InitializeOnLoad]
    public static class CoreEditor
    {
        private const string CoreFolderName = "Bumi Mobile Core";
        private const string DefaultCoreFolder = "Assets/Bumi Mobile Core";
        private const string DefaultCoreSettingsPath = DefaultCoreFolder + "/Core Settings.asset";

        public static string FolderCore { get; private set; }
        public static string FolderCoreModules => Path.Combine(FolderCore, "Modules");
        public static string FolderData { get; private set; }
        public static string FolderScenes { get; private set; }

        public static bool UseCustomInspector { get; private set; } = true;
        public static bool UseHierarchyIcons { get; private set; } = true;

        public static bool AutoLoadInitializer { get; private set; } = true;
        public static string InitSceneName { get; private set; } = "Init";

        public static Color AdsDummyBackgroundColor { get; private set; } = new Color(0.2f, 0.2f, 0.3f);
        public static Color AdsDummyMainColor { get; private set; } = new Color(0.2f, 0.3f, 0.7f);

        public static bool ShowBumiMobilePromotions { get; private set; } = true;

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

                Debug.LogWarning("[Bumi Mobile Core]: Core Settings asset cannot be found in the project. This asset is required for proper module functionality.");

                coreSettings = ScriptableObject.CreateInstance<CoreSettings>();

                EnsureDefaultCoreFolder();

                AssetDatabase.CreateAsset(coreSettings, DefaultCoreSettingsPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else
            {
                coreSettings = EnsureProjectOwnedSettings(coreSettings);
            }

            FolderCore = AssetDatabase.GetAssetPath(coreSettings).Replace(coreSettings.name + ".asset", "");
            ApplySettings(coreSettings);
        }

        private static void EnsureDefaultCoreFolder()
        {
            if (!AssetDatabase.IsValidFolder(DefaultCoreFolder))
            {
                AssetDatabase.CreateFolder("Assets", CoreFolderName);
            }
        }

        private static CoreSettings EnsureProjectOwnedSettings(CoreSettings settings)
        {
            string assetPath = AssetDatabase.GetAssetPath(settings);
            if (!assetPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
            {
                return settings;
            }

            CoreSettings existingProjectAsset = AssetDatabase.LoadAssetAtPath<CoreSettings>(DefaultCoreSettingsPath);
            if (existingProjectAsset != null)
            {
                return existingProjectAsset;
            }

            EnsureDefaultCoreFolder();

            if (!AssetDatabase.CopyAsset(assetPath, DefaultCoreSettingsPath))
            {
                CoreSettings clone = ScriptableObject.CreateInstance<CoreSettings>();
                EditorUtility.CopySerialized(settings, clone);
                AssetDatabase.CreateAsset(clone, DefaultCoreSettingsPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Bumi Mobile Core]: Core Settings asset has been copied into Assets for easier editing.");
            return AssetDatabase.LoadAssetAtPath<CoreSettings>(DefaultCoreSettingsPath);
        }

        public static void ApplySettings(CoreSettings settings)
        {
            FolderData = settings.DataFolder;
            FolderScenes = settings.ScenesFolder;
            InitSceneName = settings.InitSceneName;
            AutoLoadInitializer = settings.AutoLoadInitializer;
            UseCustomInspector = settings.UseCustomInspector;
            UseHierarchyIcons = settings.UseHierarchyIcons;
            AdsDummyBackgroundColor = settings.AdsDummyBackgroundColor;
            AdsDummyMainColor = settings.AdsDummyMainColor;
        }

        public static string FormatPath(string path)
        {
            return path.Replace("{CORE_MODULES}", FolderCoreModules)
                       .Replace("{CORE_DATA}", FolderData)
                       .Replace("{CORE}", FolderCore);
        }
    }
}
