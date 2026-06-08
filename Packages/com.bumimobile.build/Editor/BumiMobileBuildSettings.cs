using System.IO;
using UnityEditor;
using UnityEngine;

namespace BumiMobile.Build
{
    [FilePath(SettingsFilePath, FilePathAttribute.Location.ProjectFolder)]
    public sealed class BumiMobileBuildSettings : ScriptableSingleton<BumiMobileBuildSettings>
    {
        internal const string SettingsFilePath = "ProjectSettings/BumiMobileBuildSettings.asset";
        internal const string DefaultInternalTestLabel = "Development - Internal Test";
        internal const string DefaultCheatLabel = "Development - Cheat";
        internal const string DefaultProductionLabel = "Production";
        internal const string DefaultCheatScriptingDefine = "BUMI_CHEAT_BUILD";

        [SerializeField] private bool enablePreBuildWindow = true;
        [SerializeField] private string internalTestLabel = DefaultInternalTestLabel;
        [SerializeField] private string cheatLabel = DefaultCheatLabel;
        [SerializeField] private string productionLabel = DefaultProductionLabel;
        [SerializeField] private string cheatScriptingDefine = DefaultCheatScriptingDefine;

        public bool EnablePreBuildWindow => enablePreBuildWindow;
        public string InternalTestLabel => internalTestLabel;
        public string CheatLabel => cheatLabel;
        public string ProductionLabel => productionLabel;
        public string CheatScriptingDefine => cheatScriptingDefine;

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
            internalTestLabel = DefaultInternalTestLabel;
            cheatLabel = DefaultCheatLabel;
            productionLabel = DefaultProductionLabel;
            cheatScriptingDefine = DefaultCheatScriptingDefine;
        }

        internal static bool IsValidScriptingDefine(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string trimmedValue = value.Trim();
            if (!IsIdentifierStart(trimmedValue[0]))
                return false;

            for (int i = 1; i < trimmedValue.Length; i++)
            {
                if (!IsIdentifierPart(trimmedValue[i]))
                    return false;
            }

            return true;
        }

        private static bool IsIdentifierStart(char value)
        {
            return value == '_' || char.IsLetter(value);
        }

        private static bool IsIdentifierPart(char value)
        {
            return value == '_' || char.IsLetterOrDigit(value);
        }
    }
}
