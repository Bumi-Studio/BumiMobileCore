using System;
using System.Collections.Generic;
using UnityEditor;

namespace BumiMobile.Build
{
    internal enum BuildPreset
    {
        DevelopmentInternalTest = 0,
        DevelopmentCheat = 1,
        Production = 2
    }

    internal static class BuildConfigurationUtility
    {
        internal static BuildPlayerOptions Apply(
            BuildPlayerOptions options,
            BuildPreset selectedPreset,
            string cheatScriptingDefine)
        {
            bool isProduction = selectedPreset == BuildPreset.Production;
            bool enableCheatBuild = selectedPreset == BuildPreset.DevelopmentCheat;

            options.options = isProduction
                ? options.options & ~BuildOptions.Development
                : options.options | BuildOptions.Development;

            options.extraScriptingDefines = UpdateScriptingDefines(
                options.extraScriptingDefines,
                cheatScriptingDefine,
                enableCheatBuild);

            return options;
        }

        internal static string[] UpdateScriptingDefines(
            string[] sourceDefines,
            string cheatScriptingDefine,
            bool enableCheatBuild)
        {
            string normalizedCheatDefine = string.IsNullOrWhiteSpace(cheatScriptingDefine)
                ? string.Empty
                : cheatScriptingDefine.Trim();
            var result = new List<string>();

            if (sourceDefines != null)
            {
                for (int i = 0; i < sourceDefines.Length; i++)
                {
                    string define = sourceDefines[i];
                    if (!string.IsNullOrEmpty(normalizedCheatDefine) &&
                        string.Equals(define, normalizedCheatDefine, StringComparison.Ordinal))
                        continue;

                    result.Add(define);
                }
            }

            if (enableCheatBuild && !string.IsNullOrEmpty(normalizedCheatDefine))
            {
                result.Add(normalizedCheatDefine);
            }

            return result.ToArray();
        }
    }
}
