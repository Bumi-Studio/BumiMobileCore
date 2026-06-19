using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;

namespace BumiMobile.Build
{
    [InitializeOnLoad]
    internal sealed class BuildVersionConfirmWindow : EditorWindow
    {
        private const string WindowTitle = "Confirm Build Profile";
        private const string InProjectKeystorePrefix = "{inproject}: ";
        private const string PasswordPreferencePrefix = "BumiMobile.Build.PreBuildWindow.Profile.";
        private const string KeystorePasswordSuffix = ".KeystorePassword";
        private const string KeyAliasPasswordSuffix = ".KeyAliasPassword";

        private const string SessionPendingWindowKey = "BumiMobile.Build.PreBuildWindow.Pending";
        private const string SessionWaitingForReloadKey = "BumiMobile.Build.PreBuildWindow.WaitingForReload";
        private const string SessionSwitchStartedKey = "BumiMobile.Build.PreBuildWindow.SwitchStarted";
        private const string SessionProfileGuidKey = "BumiMobile.Build.PreBuildWindow.ProfileGuid";
        private const string SessionLocationPathKey = "BumiMobile.Build.PreBuildWindow.LocationPath";
        private const string SessionManifestPathKey = "BumiMobile.Build.PreBuildWindow.ManifestPath";
        private const string SessionBuildOptionsKey = "BumiMobile.Build.PreBuildWindow.BuildOptions";
        private const string SessionSwitchAttemptKey = "BumiMobile.Build.PreBuildWindow.SwitchAttempt";
        private const string SessionBuildAfterSwitchKey = "BumiMobile.Build.PreBuildWindow.BuildAfterSwitch";
        private const int MaxProfileSwitchAttempts = 5;

        private const double ProfileSwitchFallbackDelaySeconds = 2d;

        private const BuildOptions BuildRequestOptionMask =
            BuildOptions.AutoRunPlayer |
            BuildOptions.ShowBuiltPlayer |
            BuildOptions.CleanBuildCache |
            BuildOptions.BuildScriptsOnly |
            BuildOptions.PatchPackage |
            BuildOptions.ComputeCRC |
            BuildOptions.StrictMode |
            BuildOptions.NoUniqueIdentifier |
            BuildOptions.DetailedBuildReport;

        private sealed class ProfileEntry
        {
            public BuildProfile Profile;
            public string Guid;
            public string AssetPath;
            public string DisplayName;
        }

        private readonly List<ProfileEntry> profiles = new List<ProfileEntry>();

        private BuildPlayerOptions buildOptions;
        private string[] profileLabels = Array.Empty<string>();
        private int selectedProfileIndex = -1;
        private bool selectedHasCustomPlayerSettings;
        private bool developmentBuild;
        private bool buildAppBundle;

        private string version;
        private string bundleVersionCode;
        private bool useCustomKeystore;
        private string keystorePath;
        private string keyAlias;
        private string keystorePassword;
        private string keyAliasPassword;

        private string loadedVersion;
        private string loadedBundleVersionCode;
        private bool loadedUseCustomKeystore;
        private string loadedKeystorePath;
        private string loadedKeyAlias;
        private string loadedKeystorePassword;
        private string loadedKeyAliasPassword;

        private string validationMessage;
        private Vector2 scrollPosition;
        private bool initialized;

        static BuildVersionConfirmWindow()
        {
            BumiMobileBuildSettings.instance.EnsureSaved();
            BuildPlayerWindow.RegisterBuildPlayerHandler(HandleBuild);
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            EditorApplication.update += RestorePendingWindow;
        }

        private static void HandleBuild(BuildPlayerOptions options)
        {
            BumiMobileBuildSettings settings = BumiMobileBuildSettings.instance;
            if (!settings.EnablePreBuildWindow || options.target != BuildTarget.Android)
            {
                BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
                return;
            }

            ClearPendingWindowState();
            SaveBuildRequest(options);
            OpenWindow(options, null);
        }

        private static void OpenWindow(BuildPlayerOptions options, string preferredProfileGuid)
        {
            var window = CreateInstance<BuildVersionConfirmWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(540f, 410f);
            window.maxSize = new Vector2(540f, 590f);

            if (!window.Initialize(options, preferredProfileGuid))
            {
                DestroyImmediate(window);
                return;
            }

            window.ShowModalUtility();
        }

        private bool Initialize(BuildPlayerOptions options, string preferredProfileGuid)
        {
            buildOptions = options;
            DiscoverProfiles();

            if (profiles.Count == 0)
            {
                validationMessage = "No Android Build Profiles were found.";
                CaptureLoadedValues();
                initialized = true;
                return true;
            }

            string profileGuid = !string.IsNullOrEmpty(preferredProfileGuid)
                ? preferredProfileGuid
                : GetProfileGuid(BuildProfile.GetActiveBuildProfile());

            selectedProfileIndex = FindProfileIndex(profileGuid);
            if (selectedProfileIndex < 0)
            {
                selectedProfileIndex = FindBestMatchingProfileIndex(options);
            }

            LoadSelectedProfileSettings();
            CaptureLoadedValues();
            initialized = true;
            return true;
        }

        private void OnFocus()
        {
            if (!initialized)
                return;

            string selectedGuid = GetSelectedProfile()?.Guid;
            bool hasUnsavedChanges = HasUnsavedChanges();

            DiscoverProfiles();
            selectedProfileIndex = FindProfileIndex(selectedGuid);

            if (selectedProfileIndex < 0)
            {
                selectedProfileIndex = FindProfileIndex(GetProfileGuid(BuildProfile.GetActiveBuildProfile()));
            }

            if (!hasUnsavedChanges)
            {
                LoadSelectedProfileSettings();
                CaptureLoadedValues();
            }

            Repaint();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawBuildProfileSection();
            DrawBuildVersionSection();
            DrawKeystoreSection();

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
                    ClearBuildRequestSession();
                    Close();
                }

                using (new EditorGUI.DisabledScope(GetSelectedProfile() == null))
                {
                    if (GUILayout.Button("Build", GUILayout.Width(90f)))
                    {
                        SaveAndBuild();
                    }
                }
            }
        }

        private void DrawBuildProfileSection()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Build Profile", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (profiles.Count == 0)
                {
                    EditorGUILayout.HelpBox("No Android Build Profiles were found in the project.", MessageType.Error);
                    return;
                }

                int nextProfileIndex = EditorGUILayout.Popup("Profile", selectedProfileIndex, profileLabels);
                if (nextProfileIndex != selectedProfileIndex)
                {
                    if (HasUnsavedChanges() &&
                        !EditorUtility.DisplayDialog(
                            "Discard Unsaved Changes?",
                            "Switching Build Profile will discard the values currently edited in this window.",
                            "Switch Profile",
                            "Cancel"))
                    {
                        return;
                    }

                    selectedProfileIndex = nextProfileIndex;
                    LoadSelectedProfileSettings();
                    CaptureLoadedValues();
                    Repaint();
                }

                ProfileEntry selectedProfile = GetSelectedProfile();
                if (selectedProfile == null)
                    return;

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("Build Type", developmentBuild ? "Development" : "Production");
                    EditorGUILayout.TextField("Package Format", buildAppBundle ? "AAB" : "APK");
                    EditorGUILayout.TextField("Cheat", developmentBuild ? "Enabled" : "Disabled");
                }

                EditorGUILayout.LabelField(selectedProfile.AssetPath, EditorStyles.miniLabel);

                if (!selectedHasCustomPlayerSettings)
                {
                    EditorGUILayout.HelpBox(
                        "This profile does not use Customize Player Settings. Enable it in Unity Build Profiles before building.",
                        MessageType.Warning);
                }
            }
        }

        private void DrawBuildVersionSection()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Build Version", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            using (new EditorGUI.DisabledScope(!selectedHasCustomPlayerSettings))
            {
                version = EditorGUILayout.TextField("Version", version);
                bundleVersionCode = EditorGUILayout.TextField("Bundle Version Code", bundleVersionCode);
            }
        }

        private void DrawKeystoreSection()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Publishing Settings / Keystore", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            using (new EditorGUI.DisabledScope(!selectedHasCustomPlayerSettings))
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
        }

        private void SaveAndBuild()
        {
            ProfileEntry selectedProfile = GetSelectedProfile();
            if (selectedProfile == null)
            {
                SetValidationMessage("Select an Android Build Profile.");
                return;
            }

            if (!selectedHasCustomPlayerSettings)
            {
                SetValidationMessage("Enable Customize Player Settings for the selected Build Profile before building.");
                return;
            }

            string trimmedVersion = string.IsNullOrWhiteSpace(version) ? string.Empty : version.Trim();
            string trimmedKeystorePath = string.IsNullOrWhiteSpace(keystorePath) ? string.Empty : keystorePath.Trim();
            string trimmedKeyAlias = string.IsNullOrWhiteSpace(keyAlias) ? string.Empty : keyAlias.Trim();

            if (string.IsNullOrEmpty(trimmedVersion))
            {
                SetValidationMessage("Version cannot be empty.");
                return;
            }

            if (!int.TryParse(bundleVersionCode, out int parsedBundleVersionCode) || parsedBundleVersionCode <= 0)
            {
                SetValidationMessage("Bundle Version Code must be a number greater than 0.");
                return;
            }

            if (useCustomKeystore)
            {
                if (string.IsNullOrEmpty(trimmedKeystorePath))
                {
                    SetValidationMessage("Keystore Path cannot be empty when custom keystore is enabled.");
                    return;
                }

                if (!TryResolveKeystorePath(trimmedKeystorePath, out string resolvedKeystorePath) ||
                    !File.Exists(resolvedKeystorePath))
                {
                    SetValidationMessage("Keystore Path must point to an existing file.");
                    return;
                }

                if (string.IsNullOrEmpty(trimmedKeyAlias))
                {
                    SetValidationMessage("Key Alias cannot be empty when custom keystore is enabled.");
                    return;
                }

                if (string.IsNullOrEmpty(keystorePassword))
                {
                    SetValidationMessage("Keystore Password cannot be empty when custom keystore is enabled.");
                    return;
                }

                if (string.IsNullOrEmpty(keyAliasPassword))
                {
                    SetValidationMessage("Key Alias Password cannot be empty when custom keystore is enabled.");
                    return;
                }
            }

            // Edited platform settings are stored on the selected Build Profile.
            SetProfileYamlSetting(selectedProfile.Profile, "bundleVersion", trimmedVersion);
            SetProfileYamlSetting(
                selectedProfile.Profile,
                "AndroidBundleVersionCode",
                parsedBundleVersionCode.ToString(CultureInfo.InvariantCulture));
            SetProfileYamlSetting(selectedProfile.Profile, "androidUseCustomKeystore", useCustomKeystore ? "1" : "0");
            SetProfileYamlSetting(selectedProfile.Profile, "AndroidKeystoreName", ToProfileYamlString(trimmedKeystorePath));
            SetProfileYamlSetting(selectedProfile.Profile, "AndroidKeyaliasName", ToProfileYamlString(trimmedKeyAlias));
            RemoveProfileYamlSetting(selectedProfile.Profile, "AndroidUseCustomKeystore");

            EditorUtility.SetDirty(selectedProfile.Profile);
            AssetDatabase.SaveAssetIfDirty(selectedProfile.Profile);

            SaveProfilePasswords(selectedProfile.Guid);

            string outputPath = NormalizeAndroidBuildPath(buildOptions.locationPathName, buildAppBundle);
            var profileBuildOptions = new BuildPlayerWithProfileOptions
            {
                buildProfile = selectedProfile.Profile,
                locationPathName = outputPath,
                assetBundleManifestPath = buildOptions.assetBundleManifestPath,
                options = buildOptions.options & BuildRequestOptionMask
            };

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                BeginProfileSwitch(selectedProfile, true);
                return;
            }

            ClearBuildRequestSession();
            Close();

            ScheduleBuild(profileBuildOptions, keystorePassword, keyAliasPassword);
        }

        private static void ScheduleBuild(
            BuildPlayerWithProfileOptions profileBuildOptions,
            string buildKeystorePassword,
            string buildKeyAliasPassword)
        {
            EditorApplication.delayCall += () =>
            {
                try
                {
                    PlayerSettings.Android.keystorePass = buildKeystorePassword;
                    PlayerSettings.Android.keyaliasPass = buildKeyAliasPassword;
                    BuildPipeline.BuildPlayer(profileBuildOptions);
                }
                finally
                {
                    PlayerSettings.Android.keystorePass = string.Empty;
                    PlayerSettings.Android.keyaliasPass = string.Empty;
                }
            };
        }

        private void DiscoverProfiles()
        {
            profiles.Clear();

            string[] profileGuids = AssetDatabase.FindAssets("t:BuildProfile");
            for (int i = 0; i < profileGuids.Length; i++)
            {
                string guid = profileGuids[i];
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                BuildProfile profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(assetPath);

                if (profile == null || !IsAndroidProfile(profile))
                    continue;

                profiles.Add(new ProfileEntry
                {
                    Profile = profile,
                    Guid = guid,
                    AssetPath = assetPath,
                    DisplayName = profile.name
                });
            }

            profiles.Sort((left, right) =>
            {
                int nameComparison = string.Compare(
                    left.DisplayName,
                    right.DisplayName,
                    StringComparison.OrdinalIgnoreCase);

                return nameComparison != 0
                    ? nameComparison
                    : string.Compare(left.AssetPath, right.AssetPath, StringComparison.OrdinalIgnoreCase);
            });

            profileLabels = new string[profiles.Count];
            for (int i = 0; i < profiles.Count; i++)
            {
                profileLabels[i] = profiles[i].DisplayName;
            }
        }

        private static bool IsAndroidProfile(BuildProfile profile)
        {
            var serializedProfile = new SerializedObject(profile);
            SerializedProperty buildTargetProperty = serializedProfile.FindProperty("m_BuildTarget");
            if (buildTargetProperty == null)
            {
                Debug.LogWarning($"[BuildVersionConfirmWindow] BuildProfile '{profile.name}' is missing 'm_BuildTarget' property. Unity may have changed the internal serialization format.");
                return false;
            }

            return buildTargetProperty.intValue == (int)BuildTarget.Android;
        }

        private static bool HasCustomPlayerSettings(BuildProfile profile)
        {
            var serializedProfile = new SerializedObject(profile);
            SerializedProperty playerSettingsYaml = serializedProfile.FindProperty("m_PlayerSettingsYaml");
            if (playerSettingsYaml == null)
            {
                Debug.LogWarning($"[BuildVersionConfirmWindow] BuildProfile '{profile.name}' is missing 'm_PlayerSettingsYaml' property. Unity may have changed the internal serialization format.");
                return false;
            }

            SerializedProperty settings = playerSettingsYaml.FindPropertyRelative("m_Settings");
            return settings != null && settings.isArray && settings.arraySize > 0;
        }

        private int FindBestMatchingProfileIndex(BuildPlayerOptions options)
        {
            bool requestedDevelopment = (options.options & BuildOptions.Development) != 0;

            for (int i = 0; i < profiles.Count; i++)
            {
                if (TryGetSerializedDevelopmentSetting(profiles[i].Profile, out bool profileDevelopment) &&
                    profileDevelopment == requestedDevelopment)
                {
                    return i;
                }
            }

            return 0;
        }

        private static bool TryGetSerializedDevelopmentSetting(BuildProfile profile, out bool development)
        {
            var serializedProfile = new SerializedObject(profile);
            SerializedProperty platformSettings = serializedProfile.FindProperty("m_PlatformBuildProfile");
            if (platformSettings == null)
            {
                Debug.LogWarning($"[BuildVersionConfirmWindow] BuildProfile '{profile.name}' is missing 'm_PlatformBuildProfile' property. Unity may have changed the internal serialization format.");
                development = false;
                return false;
            }

            SerializedProperty developmentProperty = platformSettings.FindPropertyRelative("m_Development");
            if (developmentProperty == null)
            {
                Debug.LogWarning($"[BuildVersionConfirmWindow] BuildProfile '{profile.name}' is missing 'm_Development' property. Unity may have changed the internal serialization format.");
                development = false;
                return false;
            }

            development = developmentProperty.boolValue;
            return true;
        }

        private static bool TryGetSerializedBuildAppBundleSetting(BuildProfile profile, out bool buildAppBundle)
        {
            var serializedProfile = new SerializedObject(profile);
            SerializedProperty platformSettings = serializedProfile.FindProperty("m_PlatformBuildProfile");
            if (platformSettings == null)
            {
                Debug.LogWarning($"[BuildVersionConfirmWindow] BuildProfile '{profile.name}' is missing 'm_PlatformBuildProfile' property. Unity may have changed the internal serialization format.");
                buildAppBundle = false;
                return false;
            }

            SerializedProperty buildAppBundleProperty = platformSettings.FindPropertyRelative("m_BuildAppBundle");
            if (buildAppBundleProperty == null)
            {
                Debug.LogWarning($"[BuildVersionConfirmWindow] BuildProfile '{profile.name}' is missing 'm_BuildAppBundle' property. Unity may have changed the internal serialization format.");
                buildAppBundle = false;
                return false;
            }

            buildAppBundle = buildAppBundleProperty.boolValue;
            return true;
        }

        private void LoadSelectedProfileSettings()
        {
            ProfileEntry selectedProfile = GetSelectedProfile();
            if (selectedProfile == null)
            {
                selectedHasCustomPlayerSettings = false;
                developmentBuild = false;
                buildAppBundle = false;
                return;
            }

            selectedHasCustomPlayerSettings = HasCustomPlayerSettings(selectedProfile.Profile);

            if (!TryGetSerializedDevelopmentSetting(selectedProfile.Profile, out developmentBuild))
                developmentBuild = EditorUserBuildSettings.development;

            if (!TryGetSerializedBuildAppBundleSetting(selectedProfile.Profile, out buildAppBundle))
                buildAppBundle = EditorUserBuildSettings.buildAppBundle;

            if (TryGetProfileYamlSetting(selectedProfile.Profile, "bundleVersion", out string profileVersion))
                version = profileVersion;
            else
                version = PlayerSettings.bundleVersion;

            if (TryGetProfileYamlSetting(selectedProfile.Profile, "AndroidBundleVersionCode", out string profileBundleCode) &&
                int.TryParse(profileBundleCode, out int parsedCode))
            {
                bundleVersionCode = parsedCode.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                bundleVersionCode = PlayerSettings.Android.bundleVersionCode.ToString(CultureInfo.InvariantCulture);
            }

            if (TryGetProfileYamlSetting(selectedProfile.Profile, "androidUseCustomKeystore", out string profileUseCustomKeystore) ||
                TryGetProfileYamlSetting(selectedProfile.Profile, "AndroidUseCustomKeystore", out profileUseCustomKeystore))
            {
                useCustomKeystore =
                    string.Equals(profileUseCustomKeystore, "1", StringComparison.Ordinal) ||
                    string.Equals(profileUseCustomKeystore, "true", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                useCustomKeystore = PlayerSettings.Android.useCustomKeystore;
            }

            if (TryGetProfileYamlSetting(selectedProfile.Profile, "AndroidKeystoreName", out string profileKeystorePath))
                keystorePath = profileKeystorePath;
            else
                keystorePath = PlayerSettings.Android.keystoreName;

            if (TryGetProfileYamlSetting(selectedProfile.Profile, "AndroidKeyaliasName", out string profileKeyAlias))
                keyAlias = profileKeyAlias;
            else
                keyAlias = PlayerSettings.Android.keyaliasName;

            keystorePassword = EditorPrefs.GetString(
                GetPasswordPreferenceKey(selectedProfile.Guid, KeystorePasswordSuffix),
                string.Empty);
            keyAliasPassword = EditorPrefs.GetString(
                GetPasswordPreferenceKey(selectedProfile.Guid, KeyAliasPasswordSuffix),
                string.Empty);
            validationMessage = string.Empty;
        }

        private static bool TryGetProfileYamlSetting(BuildProfile profile, string key, out string value)
        {
            value = string.Empty;
            var serializedProfile = new SerializedObject(profile);
            SerializedProperty playerSettingsYaml = serializedProfile.FindProperty("m_PlayerSettingsYaml");
            if (playerSettingsYaml == null)
                return false;

            SerializedProperty settings = playerSettingsYaml.FindPropertyRelative("m_Settings");
            if (settings == null || !settings.isArray)
                return false;

            string searchPrefix = "|   " + key + ":";
            for (int i = 0; i < settings.arraySize; i++)
            {
                SerializedProperty lineProperty = settings.GetArrayElementAtIndex(i);
                SerializedProperty lineText = lineProperty?.FindPropertyRelative("line");
                if (lineText == null || string.IsNullOrEmpty(lineText.stringValue))
                    continue;

                string line = lineText.stringValue;
                if (line.StartsWith(searchPrefix, StringComparison.Ordinal))
                {
                    value = FromProfileYamlString(line.Substring(searchPrefix.Length).Trim());
                    return true;
                }
            }

            return false;
        }

        private static void SetProfileYamlSetting(BuildProfile profile, string key, string value)
        {
            var serializedProfile = new SerializedObject(profile);
            SerializedProperty playerSettingsYaml = serializedProfile.FindProperty("m_PlayerSettingsYaml");
            if (playerSettingsYaml == null)
            {
                Debug.LogWarning($"[BuildVersionConfirmWindow] Cannot set '{key}' - profile '{profile.name}' has no m_PlayerSettingsYaml.");
                return;
            }

            SerializedProperty settings = playerSettingsYaml.FindPropertyRelative("m_Settings");
            if (settings == null || !settings.isArray)
            {
                Debug.LogWarning($"[BuildVersionConfirmWindow] Cannot set '{key}' - profile '{profile.name}' has no m_Settings array.");
                return;
            }

            string searchPrefix = "|   " + key + ":";
            for (int i = 0; i < settings.arraySize; i++)
            {
                SerializedProperty lineProperty = settings.GetArrayElementAtIndex(i);
                SerializedProperty lineText = lineProperty?.FindPropertyRelative("line");
                if (lineText == null || string.IsNullOrEmpty(lineText.stringValue))
                    continue;

                if (lineText.stringValue.StartsWith(searchPrefix, StringComparison.Ordinal))
                {
                    lineText.stringValue = searchPrefix + " " + value;
                    serializedProfile.ApplyModifiedProperties();
                    return;
                }
            }

            settings.InsertArrayElementAtIndex(settings.arraySize);
            SerializedProperty newLineProperty = settings.GetArrayElementAtIndex(settings.arraySize - 1);
            SerializedProperty newLineText = newLineProperty?.FindPropertyRelative("line");
            if (newLineText != null)
            {
                newLineText.stringValue = searchPrefix + " " + value;
                serializedProfile.ApplyModifiedProperties();
            }
        }

        private static void RemoveProfileYamlSetting(BuildProfile profile, string key)
        {
            var serializedProfile = new SerializedObject(profile);
            SerializedProperty playerSettingsYaml = serializedProfile.FindProperty("m_PlayerSettingsYaml");
            SerializedProperty settings = playerSettingsYaml?.FindPropertyRelative("m_Settings");
            if (settings == null || !settings.isArray)
                return;

            string searchPrefix = "|   " + key + ":";
            for (int i = settings.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty lineProperty = settings.GetArrayElementAtIndex(i);
                SerializedProperty lineText = lineProperty?.FindPropertyRelative("line");
                if (lineText != null && lineText.stringValue.StartsWith(searchPrefix, StringComparison.Ordinal))
                {
                    settings.DeleteArrayElementAtIndex(i);
                }
            }

            serializedProfile.ApplyModifiedProperties();
        }

        private static string ToProfileYamlString(string value)
        {
            return "'" + (value ?? string.Empty).Replace("'", "''") + "'";
        }

        private static string FromProfileYamlString(string value)
        {
            if (value.Length >= 2 && value[0] == '\'' && value[value.Length - 1] == '\'')
            {
                return value.Substring(1, value.Length - 2).Replace("''", "'");
            }

            return value;
        }

        private void SaveProfilePasswords(string profileGuid)
        {
            EditorPrefs.SetString(
                GetPasswordPreferenceKey(profileGuid, KeystorePasswordSuffix),
                keystorePassword ?? string.Empty);
            EditorPrefs.SetString(
                GetPasswordPreferenceKey(profileGuid, KeyAliasPasswordSuffix),
                keyAliasPassword ?? string.Empty);
        }

        private static string GetPasswordPreferenceKey(string profileGuid, string suffix)
        {
            return PasswordPreferencePrefix + profileGuid + suffix;
        }

        private void BeginProfileSwitch(ProfileEntry profile, bool buildAfterSwitch)
        {
            if (profile == null)
                return;

            SaveBuildRequest(buildOptions);
            SessionState.SetString(SessionProfileGuidKey, profile.Guid);
            SessionState.SetBool(SessionPendingWindowKey, true);
            SessionState.SetBool(SessionWaitingForReloadKey, true);
            SessionState.SetBool(SessionBuildAfterSwitchKey, buildAfterSwitch);
            SessionState.SetInt(SessionSwitchAttemptKey, 1);
            SessionState.SetString(
                SessionSwitchStartedKey,
                EditorApplication.timeSinceStartup.ToString(CultureInfo.InvariantCulture));

            Close();

            try
            {
                BuildProfile.SetActiveBuildProfile(profile.Profile);
            }
            catch (Exception exception)
            {
                ClearPendingWindowState();
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Build Profile Switch Failed", exception.Message, "OK");
                return;
            }

            EditorApplication.delayCall += RestorePendingWindow;
        }

        private static void OnAfterAssemblyReload()
        {
            if (SessionState.GetBool(SessionPendingWindowKey, false))
            {
                SessionState.SetBool(SessionWaitingForReloadKey, false);
            }
        }

        private static void RestorePendingWindow()
        {
            if (!SessionState.GetBool(SessionPendingWindowKey, false))
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            int attempt = SessionState.GetInt(SessionSwitchAttemptKey, 0);
            if (attempt >= MaxProfileSwitchAttempts)
            {
                ClearPendingWindowState();
                EditorUtility.DisplayDialog(
                    "Build Profile Switch Failed",
                    "The Build Profile could not be activated after multiple attempts. " +
                    "Please activate it manually in Unity Build Profiles and try again.",
                    "OK");
                return;
            }

            if (SessionState.GetBool(SessionWaitingForReloadKey, false))
            {
                string startedValue = SessionState.GetString(SessionSwitchStartedKey, string.Empty);
                if (double.TryParse(
                        startedValue,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double switchStarted) &&
                    EditorApplication.timeSinceStartup - switchStarted < ProfileSwitchFallbackDelaySeconds)
                {
                    return;
                }

                SessionState.SetBool(SessionWaitingForReloadKey, false);
            }

            string profileGuid = SessionState.GetString(SessionProfileGuidKey, string.Empty);
            string profilePath = AssetDatabase.GUIDToAssetPath(profileGuid);
            BuildProfile requestedProfile = AssetDatabase.LoadAssetAtPath<BuildProfile>(profilePath);

            if (requestedProfile != null && GetProfileGuid(BuildProfile.GetActiveBuildProfile()) != profileGuid)
            {
                SessionState.SetBool(SessionWaitingForReloadKey, true);
                SessionState.SetInt(SessionSwitchAttemptKey, attempt + 1);
                SessionState.SetString(
                    SessionSwitchStartedKey,
                    EditorApplication.timeSinceStartup.ToString(CultureInfo.InvariantCulture));

                try
                {
                    BuildProfile.SetActiveBuildProfile(requestedProfile);
                }
                catch (Exception exception)
                {
                    ClearPendingWindowState();
                    Debug.LogException(exception);
                    EditorUtility.DisplayDialog("Build Profile Switch Failed", exception.Message, "OK");
                }

                return;
            }

            BuildPlayerOptions options = LoadBuildRequest();
            bool buildAfterSwitch = SessionState.GetBool(SessionBuildAfterSwitchKey, false);
            ClearPendingWindowState();

            if (buildAfterSwitch)
            {
                ResumeBuildAfterProfileSwitch(options, requestedProfile, profileGuid);
                return;
            }

            OpenWindow(options, profileGuid);
        }

        private static void ResumeBuildAfterProfileSwitch(
            BuildPlayerOptions options,
            BuildProfile profile,
            string profileGuid)
        {
            if (profile == null)
            {
                ClearBuildRequestSession();
                EditorUtility.DisplayDialog(
                    "Build Profile Missing",
                    "The selected Build Profile could not be loaded after switching platforms.",
                    "OK");
                return;
            }

            bool profileBuildAppBundle = TryGetSerializedBuildAppBundleSetting(profile, out bool buildAppBundle)
                ? buildAppBundle
                : EditorUserBuildSettings.buildAppBundle;

            var profileBuildOptions = new BuildPlayerWithProfileOptions
            {
                buildProfile = profile,
                locationPathName = NormalizeAndroidBuildPath(options.locationPathName, profileBuildAppBundle),
                assetBundleManifestPath = options.assetBundleManifestPath,
                options = options.options & BuildRequestOptionMask
            };

            string buildKeystorePassword = EditorPrefs.GetString(
                GetPasswordPreferenceKey(profileGuid, KeystorePasswordSuffix),
                string.Empty);
            string buildKeyAliasPassword = EditorPrefs.GetString(
                GetPasswordPreferenceKey(profileGuid, KeyAliasPasswordSuffix),
                string.Empty);

            ClearBuildRequestSession();
            ScheduleBuild(profileBuildOptions, buildKeystorePassword, buildKeyAliasPassword);
        }

        private static void SaveBuildRequest(BuildPlayerOptions options)
        {
            SessionState.SetString(SessionLocationPathKey, options.locationPathName ?? string.Empty);
            SessionState.SetString(SessionManifestPathKey, options.assetBundleManifestPath ?? string.Empty);
            SessionState.SetInt(SessionBuildOptionsKey, (int)options.options);
        }

        private static BuildPlayerOptions LoadBuildRequest()
        {
            return new BuildPlayerOptions
            {
                locationPathName = SessionState.GetString(SessionLocationPathKey, string.Empty),
                assetBundleManifestPath = SessionState.GetString(SessionManifestPathKey, string.Empty),
                options = (BuildOptions)SessionState.GetInt(SessionBuildOptionsKey, 0)
            };
        }

        private static void ClearPendingWindowState()
        {
            SessionState.SetBool(SessionPendingWindowKey, false);
            SessionState.SetBool(SessionWaitingForReloadKey, false);
            SessionState.SetBool(SessionBuildAfterSwitchKey, false);
            SessionState.EraseString(SessionSwitchStartedKey);
            SessionState.EraseString(SessionProfileGuidKey);
            SessionState.EraseInt(SessionSwitchAttemptKey);
        }

        private static void ClearBuildRequestSession()
        {
            ClearPendingWindowState();
            SessionState.EraseString(SessionLocationPathKey);
            SessionState.EraseString(SessionManifestPathKey);
            SessionState.EraseInt(SessionBuildOptionsKey);
        }

        private void CaptureLoadedValues()
        {
            loadedVersion = version;
            loadedBundleVersionCode = bundleVersionCode;
            loadedUseCustomKeystore = useCustomKeystore;
            loadedKeystorePath = keystorePath;
            loadedKeyAlias = keyAlias;
            loadedKeystorePassword = keystorePassword;
            loadedKeyAliasPassword = keyAliasPassword;
        }

        private bool HasUnsavedChanges()
        {
            return !string.Equals(version, loadedVersion, StringComparison.Ordinal) ||
                   !string.Equals(bundleVersionCode, loadedBundleVersionCode, StringComparison.Ordinal) ||
                   useCustomKeystore != loadedUseCustomKeystore ||
                   !string.Equals(keystorePath, loadedKeystorePath, StringComparison.Ordinal) ||
                   !string.Equals(keyAlias, loadedKeyAlias, StringComparison.Ordinal) ||
                   !string.Equals(keystorePassword, loadedKeystorePassword, StringComparison.Ordinal) ||
                   !string.Equals(keyAliasPassword, loadedKeyAliasPassword, StringComparison.Ordinal);
        }

        private ProfileEntry GetSelectedProfile()
        {
            return selectedProfileIndex >= 0 && selectedProfileIndex < profiles.Count
                ? profiles[selectedProfileIndex]
                : null;
        }

        private int FindProfileIndex(string profileGuid)
        {
            if (string.IsNullOrEmpty(profileGuid))
                return -1;

            for (int i = 0; i < profiles.Count; i++)
            {
                if (profiles[i].Guid == profileGuid)
                    return i;
            }

            return -1;
        }

        private static string GetProfileGuid(BuildProfile profile)
        {
            if (profile == null)
                return string.Empty;

            string profilePath = AssetDatabase.GetAssetPath(profile);
            return string.IsNullOrEmpty(profilePath)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(profilePath);
        }

        private void SetValidationMessage(string message)
        {
            validationMessage = message;
            Repaint();
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

        private static string NormalizeAndroidBuildPath(string outputPath, bool appBundle)
        {
            if (string.IsNullOrEmpty(outputPath))
                return outputPath;

            string expectedExtension = appBundle ? ".aab" : ".apk";
            string currentExtension = Path.GetExtension(outputPath);

            if (string.Equals(currentExtension, ".apk", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(currentExtension, ".aab", StringComparison.OrdinalIgnoreCase))
            {
                return Path.ChangeExtension(outputPath, expectedExtension);
            }

            return outputPath;
        }
    }
}
