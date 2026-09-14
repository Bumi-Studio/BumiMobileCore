using System.IO;
using UnityEditor;
using UnityEngine;

namespace BumiMobile.Build
{
    [FilePath(SettingsFilePath, FilePathAttribute.Location.ProjectFolder)]
    public sealed class BumiMobileBuildSettings : ScriptableSingleton<BumiMobileBuildSettings>
    {
        internal const string SettingsFilePath = "ProjectSettings/BumiMobileBuildSettings.asset";

        [SerializeField] private bool enablePreBuildWindow = true;

        public bool EnablePreBuildWindow => enablePreBuildWindow;

        internal void EnsureSaved()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string settingsPath = Path.Combine(projectRoot, SettingsFilePath);

            if (!File.Exists(settingsPath))
            {
                Save(true);
            }
        }

        internal void SaveSettings()
        {
            Save(true);
        }

        internal void ResetToDefaults()
        {
            enablePreBuildWindow = true;
        }
    }
}
