using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;

namespace BumiMobile
{
    public class CorePackageManagerWindow : EditorWindow
    {
        private const string WindowTitle = "Bumi Core Packages";
        private const string RepositoryUrl = "https://github.com/Bumi-Studio/BumiMobileCore.git";

        private readonly List<PackageDefinition> packageCatalog = PackageDefinition.CreateDefaults();
        private readonly Dictionary<string, PackageInfo> installedPackages = new Dictionary<string, PackageInfo>();
        private readonly Dictionary<string, string> versionInputs = new Dictionary<string, string>();

        private Vector2 scrollPosition;
        private AddRequest pendingRequest;
        private string requestPackageName;
        private string statusMessage;
        private double statusMessageUntil;

        [MenuItem("Window/Bumi Mobile Core/Package Manager", priority = 902)]
        private static void Open()
        {
            GetWindow<CorePackageManagerWindow>(WindowTitle).Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent(WindowTitle, EditorGUIUtility.IconContent("d_Package Manager@2x").image);
            RefreshInstalledPackages();
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (pendingRequest == null)
            {
                return;
            }

            if (!pendingRequest.IsCompleted)
            {
                return;
            }

            if (pendingRequest.Status == StatusCode.Success)
            {
                statusMessage = $"{requestPackageName} installed ({pendingRequest.Result.version}).";
                statusMessageUntil = EditorApplication.timeSinceStartup + 5f;
                RefreshInstalledPackages();
            }
            else if (pendingRequest.Status >= StatusCode.Failure)
            {
                statusMessage = pendingRequest.Error != null ? pendingRequest.Error.message : "Unknown installation error.";
                statusMessageUntil = EditorApplication.timeSinceStartup + 8f;
            }

            pendingRequest = null;
            requestPackageName = null;
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.Space();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach (PackageDefinition definition in packageCatalog)
            {
                DrawPackageCard(definition);
            }
            EditorGUILayout.EndScrollView();

            DrawStatusBar();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                RefreshInstalledPackages();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Open manifest", EditorStyles.toolbarButton))
            {
                string manifestPath = PackageDefinition.GetManifestPath();
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<TextAsset>(manifestPath);
                if (Selection.activeObject == null)
                {
                    EditorUtility.RevealInFinder(manifestPath);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPackageCard(PackageDefinition definition)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField(definition.DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(definition.Description, EditorStyles.wordWrappedLabel);

            bool isInstalled = installedPackages.TryGetValue(definition.Name, out PackageInfo installedInfo);
            string installedVersion = isInstalled ? installedInfo.version : "-";

            if (!definition.IsEmbedded)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Target tag/version");
                versionInputs[definition.Name] = EditorGUILayout.TextField(versionInputs[definition.Name]);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = false;
                EditorGUILayout.LabelField("Installed", GUILayout.Width(80));
                EditorGUILayout.LabelField(installedVersion);
                GUI.enabled = true;

                GUILayout.FlexibleSpace();

                if (definition.IsEmbedded)
                {
                    GUILayout.Label("Included in Core", EditorStyles.miniBoldLabel);
                }
                else
                {
                    DrawActionButton(definition, isInstalled, installedVersion);
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3f);
        }

        private void DrawActionButton(PackageDefinition definition, bool isInstalled, string installedVersion)
        {
            string desiredVersion = versionInputs[definition.Name];
            bool hasDifferentVersion = !isInstalled || !PackageDefinition.IsSameVersion(installedVersion, desiredVersion);
            bool canInstall = pendingRequest == null && hasDifferentVersion && !string.IsNullOrWhiteSpace(desiredVersion);
            string buttonText = isInstalled ? (hasDifferentVersion ? "Update" : "Installed") : "Install";

            using (new EditorGUI.DisabledScope(!canInstall))
            {
                if (GUILayout.Button(buttonText, GUILayout.Width(110)))
                {
                    InstallPackage(definition, desiredVersion);
                }
            }
        }

        private void DrawStatusBar()
        {
            if (string.IsNullOrEmpty(statusMessage) || EditorApplication.timeSinceStartup > statusMessageUntil)
            {
                return;
            }

            EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
        }

        private void InstallPackage(PackageDefinition definition, string versionTag)
        {
            string gitUrl = definition.BuildGitUrl(RepositoryUrl, versionTag);
            pendingRequest = Client.Add(gitUrl);
            requestPackageName = definition.DisplayName;
            statusMessage = $"Installing {definition.DisplayName}...";
            statusMessageUntil = double.MaxValue;
        }

        private void RefreshInstalledPackages()
        {
            installedPackages.Clear();
            foreach (PackageInfo packageInfo in PackageInfo.GetAllRegisteredPackages())
            {
                if (!installedPackages.ContainsKey(packageInfo.name))
                {
                    installedPackages.Add(packageInfo.name, packageInfo);
                }
            }

            foreach (PackageDefinition definition in packageCatalog)
            {
                if (!versionInputs.ContainsKey(definition.Name))
                {
                    versionInputs.Add(definition.Name, definition.DefaultVersion);
                }
            }

            Repaint();
        }

        [Serializable]
        private class PackageDefinition
        {
            public string Name;
            public string DisplayName;
            public string Description;
            public string PackagePath;
            public string DefaultVersion;
            public bool IsEmbedded;

            public PackageDefinition(string name, string displayName, string description, string path, string defaultVersion, bool embedded = false)
            {
                Name = name;
                DisplayName = displayName;
                Description = description;
                PackagePath = path;
                DefaultVersion = defaultVersion;
                IsEmbedded = embedded;
            }

            public static List<PackageDefinition> CreateDefaults()
            {
                const string defaultVersion = "main";
                return new List<PackageDefinition>
                {
                    new PackageDefinition("com.bumimobile.core", "Core", "Base tooling, settings, and utilities. Already embedded in this project.", "Packages/com.bumimobile.core", defaultVersion, true),
                    new PackageDefinition("com.bumimobile.audio", "Audio", "Audio systems, mixers, and helpers.", "Packages/com.bumimobile.audio", defaultVersion),
                    new PackageDefinition("com.bumimobile.currency", "Currency", "Currency definitions and handlers.", "Packages/com.bumimobile.currency", defaultVersion),
                    new PackageDefinition("com.bumimobile.defines", "Defines", "Shared scripting defines and configuration presets.", "Packages/com.bumimobile.defines", defaultVersion),
                    new PackageDefinition("com.bumimobile.haptic", "Haptic", "Haptic feedback abstractions and presets.", "Packages/com.bumimobile.haptic", defaultVersion),
                    new PackageDefinition("com.bumimobile.localization", "Localization", "Localization data pipelines and helpers.", "Packages/com.bumimobile.localization", defaultVersion),
                    new PackageDefinition("com.bumimobile.monetization", "Monetization", "Ads and IAP settings UI plus runtime glue.", "Packages/com.bumimobile.monetization", defaultVersion),
                    new PackageDefinition("com.bumimobile.nativeshare", "Native Share", "Sharing bridges for iOS/Android.", "Packages/com.bumimobile.nativeshare", defaultVersion),
                    new PackageDefinition("com.bumimobile.pool", "Pool", "Object pooling utilities.", "Packages/com.bumimobile.pool", defaultVersion),
                    new PackageDefinition("com.bumimobile.pushnotification", "Push Notification", "Push notification bridges and helpers.", "Packages/com.bumimobile.pushnotification", defaultVersion),
                    new PackageDefinition("com.bumimobile.save", "Save", "Save system entry points and persistence helpers.", "Packages/com.bumimobile.save", defaultVersion),
                    new PackageDefinition("com.bumimobile.skins", "Skins", "Skin/theme data structures and runtime.", "Packages/com.bumimobile.skins", defaultVersion),
                    new PackageDefinition("com.bumimobile.ui", "UI", "Common UI widgets and theming.", "Packages/com.bumimobile.ui", defaultVersion),
                    new PackageDefinition("com.bumimobile.utilities", "Utilities", "Extra utility classes shared across modules.", "Packages/com.bumimobile.utilities", defaultVersion),
                    new PackageDefinition("com.bumimobile.reward", "Reward", "Reward handling and spin-wheel helpers.", "Packages/com.bumimobile.reward", defaultVersion),
                    new PackageDefinition("com.bumimobile.initializer", "Initializer", "Bootstrapper helpers and default scenes.", "Packages/com.bumimobile.initializer", defaultVersion),
                    new PackageDefinition("com.bumimobile.tween", "Tween", "Tween utilities for UI/FX.", "Packages/com.bumimobile.tween", defaultVersion)
                };
            }

            public string BuildGitUrl(string repository, string version)
            {
                return $"{repository}?path={PackagePath}#{version}";
            }

            public static bool IsSameVersion(string installed, string desired)
            {
                return string.Equals(installed, desired, StringComparison.OrdinalIgnoreCase);
            }

            public static string GetManifestPath()
            {
                string path = System.IO.Path.Combine(Application.dataPath, "../Packages/manifest.json");
                path = System.IO.Path.GetFullPath(path).Replace("\\", "/");
                string projectRelativePath = path.Replace(Application.dataPath.Replace("\\", "/") + "/../", "");
                return projectRelativePath;
            }
        }
    }
}
