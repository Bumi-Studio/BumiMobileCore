using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BumiMobile.Build
{
    [InitializeOnLoad]
    internal sealed class BuildVersionConfirmWindow : EditorWindow
    {
        private const string WindowTitle = "Confirm Build Version";
        private const string BuildPresetPreferencePrefix = "BumiMobile.Build.PreBuildWindow.BuildPreset.";
        private const string InProjectKeystorePrefix = "{inproject}: ";

        private BuildPlayerOptions buildOptions;
        private BumiMobileBuildSettings settings;
        private BuildPreset buildPreset;
        private string version;
        private string bundleVersionCode;
        private bool useCustomKeystore;
        private string keystorePath;
        private string keyAlias;
        private string keystorePassword;
        private string keyAliasPassword;
        private string validationMessage;
        private Vector2 scrollPosition;

        static BuildVersionConfirmWindow()
        {
            BumiMobileBuildSettings.instance.EnsureSaved();
            BuildPlayerWindow.RegisterBuildPlayerHandler(HandleBuild);
        }

        private static void HandleBuild(BuildPlayerOptions options)
        {
            BumiMobileBuildSettings buildSettings = BumiMobileBuildSettings.instance;

            if (!buildSettings.EnablePreBuildWindow || options.target != BuildTarget.Android)
            {
                BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
                return;
            }

            var window = CreateInstance<BuildVersionConfirmWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(520f, 430f);
            window.maxSize = new Vector2(520f, 540f);
            window.Initialize(options, buildSettings);
            window.ShowModalUtility();
        }

        private void Initialize(BuildPlayerOptions options, BumiMobileBuildSettings buildSettings)
        {
            buildOptions = options;
            settings = buildSettings;
            buildPreset = LoadBuildPreset(options, buildSettings.CheatScriptingDefine);
            version = PlayerSettings.bundleVersion;
            bundleVersionCode = PlayerSettings.Android.bundleVersionCode.ToString();
            useCustomKeystore = PlayerSettings.Android.useCustomKeystore;
            keystorePath = PlayerSettings.Android.keystoreName;
            keyAlias = PlayerSettings.Android.keyaliasName;
            keystorePassword = PlayerSettings.Android.keystorePass;
            keyAliasPassword = PlayerSettings.Android.keyaliasPass;
            validationMessage = string.Empty;
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Build Config Selection", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                string[] labels =
                {
                    settings.InternalTestLabel,
                    settings.CheatLabel,
                    settings.ProductionLabel
                };

                buildPreset = (BuildPreset)EditorGUILayout.Popup("Build Config", (int)buildPreset, labels);
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Build Version", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                version = EditorGUILayout.TextField("Version", version);
                bundleVersionCode = EditorGUILayout.TextField("Bundle Version Code", bundleVersionCode);
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Publishing Settings / Keystore", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                useCustomKeystore = EditorGUILayout.Toggle("Use Custom Keystore", useCustomKeystore);

                using (new EditorGUI.DisabledScope(!useCustomKeystore))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        keystorePath = EditorGUILayout.TextField("Keystore Path", keystorePath);

                        if (GUILayout.Button("Browse", GUILayout.Width(70f)))
                        {
                            BrowseKeystore();
                        }
                    }

                    keyAlias = EditorGUILayout.TextField("Key Alias", keyAlias);
                    keystorePassword = EditorGUILayout.PasswordField("Keystore Password", keystorePassword);
                    keyAliasPassword = EditorGUILayout.PasswordField("Key Alias Password", keyAliasPassword);
                }
            }

            if (!string.IsNullOrEmpty(validationMessage))
            {
                EditorGUILayout.HelpBox(validationMessage, MessageType.Error);
            }

            EditorGUILayout.EndScrollView();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Cancel", GUILayout.Width(90f)))
                {
                    Close();
                }

                if (GUILayout.Button("Build", GUILayout.Width(90f)))
                {
                    SaveAndBuild();
                }
            }
        }

        private void SaveAndBuild()
        {
            string trimmedVersion = string.IsNullOrWhiteSpace(version) ? string.Empty : version.Trim();
            string trimmedKeystorePath = string.IsNullOrWhiteSpace(keystorePath) ? string.Empty : keystorePath.Trim();
            string trimmedKeyAlias = string.IsNullOrWhiteSpace(keyAlias) ? string.Empty : keyAlias.Trim();
            string trimmedCheatDefine = string.IsNullOrWhiteSpace(settings.CheatScriptingDefine)
                ? string.Empty
                : settings.CheatScriptingDefine.Trim();

            if (string.IsNullOrEmpty(trimmedVersion))
            {
                ShowValidationError("Version cannot be empty.");
                return;
            }

            if (!int.TryParse(bundleVersionCode, out int parsedBundleVersionCode) || parsedBundleVersionCode <= 0)
            {
                ShowValidationError("Bundle Version Code must be a number greater than 0.");
                return;
            }

            if (!BumiMobileBuildSettings.IsValidScriptingDefine(trimmedCheatDefine))
            {
                ShowValidationError(
                    "Cheat Scripting Define must be a valid C# preprocessor identifier. Update it in Project Settings > Bumi Mobile > Build.");
                return;
            }

            if (useCustomKeystore)
            {
                if (string.IsNullOrEmpty(trimmedKeystorePath))
                {
                    ShowValidationError("Keystore Path cannot be empty when custom keystore is enabled.");
                    return;
                }

                if (!TryResolveKeystorePath(trimmedKeystorePath, out string resolvedKeystorePath) ||
                    !File.Exists(resolvedKeystorePath))
                {
                    ShowValidationError("Keystore Path must point to an existing file.");
                    return;
                }

                if (string.IsNullOrEmpty(trimmedKeyAlias))
                {
                    ShowValidationError("Key Alias cannot be empty when custom keystore is enabled.");
                    return;
                }

                if (string.IsNullOrEmpty(keystorePassword))
                {
                    ShowValidationError("Keystore Password cannot be empty when custom keystore is enabled.");
                    return;
                }

                if (string.IsNullOrEmpty(keyAliasPassword))
                {
                    ShowValidationError("Key Alias Password cannot be empty when custom keystore is enabled.");
                    return;
                }
            }

            PlayerSettings.bundleVersion = trimmedVersion;
            PlayerSettings.Android.bundleVersionCode = parsedBundleVersionCode;
            PlayerSettings.Android.useCustomKeystore = useCustomKeystore;
            PlayerSettings.Android.keystoreName = trimmedKeystorePath;
            PlayerSettings.Android.keyaliasName = trimmedKeyAlias;
            PlayerSettings.Android.keystorePass = keystorePassword;
            PlayerSettings.Android.keyaliasPass = keyAliasPassword;
            EditorPrefs.SetInt(GetBuildPresetPreferenceKey(), (int)buildPreset);
            AssetDatabase.SaveAssets();

            BuildPlayerOptions configuredOptions = BuildConfigurationUtility.Apply(
                buildOptions,
                buildPreset,
                trimmedCheatDefine);

            Close();
            EditorApplication.delayCall += () => BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(configuredOptions);
        }

        private void ShowValidationError(string message)
        {
            validationMessage = message;
            Repaint();
        }

        private static BuildPreset LoadBuildPreset(BuildPlayerOptions options, string cheatScriptingDefine)
        {
            string preferenceKey = GetBuildPresetPreferenceKey();
            if (EditorPrefs.HasKey(preferenceKey))
            {
                int savedPreset = EditorPrefs.GetInt(preferenceKey);
                if (Enum.IsDefined(typeof(BuildPreset), savedPreset))
                {
                    return (BuildPreset)savedPreset;
                }
            }

            if (ContainsDefine(options.extraScriptingDefines, cheatScriptingDefine))
                return BuildPreset.DevelopmentCheat;

            return (options.options & BuildOptions.Development) != 0
                ? BuildPreset.DevelopmentInternalTest
                : BuildPreset.Production;
        }

        private static bool ContainsDefine(string[] defines, string expectedDefine)
        {
            if (defines == null || string.IsNullOrWhiteSpace(expectedDefine))
                return false;

            string normalizedExpectedDefine = expectedDefine.Trim();
            for (int i = 0; i < defines.Length; i++)
            {
                if (string.Equals(defines[i], normalizedExpectedDefine, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private void BrowseKeystore()
        {
            string selectedPath = EditorUtility.OpenFilePanel(
                "Select Android Keystore",
                GetInitialKeystoreDirectory(),
                string.Empty);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                keystorePath = ToUnityKeystorePath(selectedPath);
            }
        }

        private string GetInitialKeystoreDirectory()
        {
            string projectPath = GetProjectPath();

            if (!TryResolveKeystorePath(keystorePath, out string resolvedKeystorePath))
                return projectPath;

            if (File.Exists(resolvedKeystorePath))
            {
                string directory = Path.GetDirectoryName(resolvedKeystorePath);
                if (!string.IsNullOrEmpty(directory))
                    return directory;
            }

            if (Directory.Exists(resolvedKeystorePath))
                return resolvedKeystorePath;

            return projectPath;
        }

        private static bool TryResolveKeystorePath(string sourcePath, out string resolvedPath)
        {
            resolvedPath = string.Empty;
            if (string.IsNullOrWhiteSpace(sourcePath))
                return false;

            try
            {
                string normalizedPath = sourcePath.Trim().Replace("\\", "/");
                if (normalizedPath.StartsWith(InProjectKeystorePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    normalizedPath = normalizedPath.Substring(InProjectKeystorePrefix.Length);
                }

                resolvedPath = Path.IsPathRooted(normalizedPath)
                    ? Path.GetFullPath(normalizedPath)
                    : Path.GetFullPath(Path.Combine(GetProjectPath(), normalizedPath));

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string ToUnityKeystorePath(string selectedPath)
        {
            string projectPath = GetProjectPath().Replace("\\", "/").TrimEnd('/');
            string fullSelectedPath = Path.GetFullPath(selectedPath).Replace("\\", "/");

            if (fullSelectedPath.StartsWith(projectPath + "/", StringComparison.OrdinalIgnoreCase))
            {
                string relativePath = fullSelectedPath.Substring(projectPath.Length + 1);
                return InProjectKeystorePrefix + relativePath;
            }

            return fullSelectedPath;
        }

        private static string GetProjectPath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static string GetBuildPresetPreferenceKey()
        {
            return BuildPresetPreferencePrefix + Application.dataPath.Replace("\\", "/");
        }
    }
}
