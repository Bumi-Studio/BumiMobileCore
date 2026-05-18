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
        private PackageDefinition activeInstallDefinition;
        private readonly Queue<PackageDefinition.PostInstallDependency> dependencyQueue = new Queue<PackageDefinition.PostInstallDependency>();
        private PackageDefinition.PostInstallDependency activeDependency;
        private bool dependencyProcessingActive;

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
                if (activeDependency != null)
                {
                    activeDependency = null;
                    TryProcessNextDependency();
                }
                else if (activeInstallDefinition != null)
                {
                    bool queued = EnqueueMissingDependencies(activeInstallDefinition);
                    activeInstallDefinition = null;
                    if (queued)
                    {
                        TryProcessNextDependency();
                    }
                }
            }
            else if (pendingRequest.Status >= StatusCode.Failure)
            {
                statusMessage = pendingRequest.Error != null ? pendingRequest.Error.message : "Unknown installation error.";
                statusMessageUntil = EditorApplication.timeSinceStartup + 8f;
                if (activeDependency != null)
                {
                    activeDependency = null;
                    dependencyQueue.Clear();
                }
                activeInstallDefinition = null;
                dependencyProcessingActive = false;
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
            if (pendingRequest != null)
            {
                return;
            }

            dependencyQueue.Clear();
            activeDependency = null;
            activeInstallDefinition = definition;
            dependencyProcessingActive = false;

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

        private bool EnqueueMissingDependencies(PackageDefinition definition)
        {
            if (definition.PostInstallDependencies == null || definition.PostInstallDependencies.Count == 0)
            {
                return false;
            }

            bool added = false;
            foreach (var dependency in definition.PostInstallDependencies)
            {
                if (!installedPackages.ContainsKey(dependency.PackageId))
                {
                    dependencyQueue.Enqueue(dependency);
                    added = true;
                }
            }
            dependencyProcessingActive |= added;
            return added;
        }

        private void TryProcessNextDependency()
        {
            if (pendingRequest != null)
            {
                return;
            }

            while (dependencyQueue.Count > 0)
            {
                var dependency = dependencyQueue.Dequeue();

                if (installedPackages.ContainsKey(dependency.PackageId))
                {
                    continue;
                }

                switch (dependency.InstallMode)
                {
                    case PackageDefinition.DependencyInstallMode.Registry:
                        StartDependencyInstall(dependency, dependency.GetIdentifier());
                        return;
                    case PackageDefinition.DependencyInstallMode.LocalEmbedded:
                        string embeddedPath = dependency.GetEmbeddedPath();
                        if (!string.IsNullOrEmpty(embeddedPath) && System.IO.Directory.Exists(embeddedPath))
                        {
                            StartDependencyInstall(dependency, $"file:{embeddedPath}");
                            return;
                        }
                        goto case PackageDefinition.DependencyInstallMode.LocalTarball;
                    case PackageDefinition.DependencyInstallMode.LocalTarball:
                        int choice = EditorUtility.DisplayDialogComplex(
                            dependency.DisplayName ?? dependency.PackageId,
                            $"{dependency.DisplayName ?? dependency.PackageId} is required by {WindowTitle}. Select the downloaded .tgz to install it.",
                            "Select .tgz…",
                            "Skip",
                            "Cancel");

                        if (choice == 1)
                        {
                            // Skip
                            continue;
                        }

                        if (choice == 2)
                        {
                            // Cancel dependency processing entirely.
                            statusMessage = "Dependency installation cancelled.";
                            statusMessageUntil = EditorApplication.timeSinceStartup + 5f;
                            dependencyQueue.Clear();
                            dependencyProcessingActive = false;
                            return;
                        }

                        string path = EditorUtility.OpenFilePanel("Select package tarball", string.Empty, "tgz");
                        if (string.IsNullOrEmpty(path))
                        {
                            continue;
                        }

                        StartDependencyInstall(dependency, $"file:{path}");
                        return;
                }
            }

            if (dependencyProcessingActive)
            {
                statusMessage = "All dependencies installed or skipped.";
                statusMessageUntil = EditorApplication.timeSinceStartup + 4f;
                dependencyProcessingActive = false;
            }
        }

        private void StartDependencyInstall(PackageDefinition.PostInstallDependency dependency, string identifier)
        {
            pendingRequest = Client.Add(identifier);
            activeDependency = dependency;
            requestPackageName = dependency.DisplayName ?? dependency.PackageId;
            statusMessage = $"Installing {requestPackageName}...";
            statusMessageUntil = double.MaxValue;
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
            public List<PostInstallDependency> PostInstallDependencies { get; }

            public PackageDefinition(string name, string displayName, string description, string path, string defaultVersion, bool embedded = false, List<PostInstallDependency> dependencies = null)
            {
                Name = name;
                DisplayName = displayName;
                Description = description;
                PackagePath = path;
                DefaultVersion = defaultVersion;
                IsEmbedded = embedded;
                PostInstallDependencies = dependencies ?? new List<PostInstallDependency>();
            }

            public static List<PackageDefinition> CreateDefaults()
            {
                const string defaultVersion = "main";
                return new List<PackageDefinition>
                {
                    new PackageDefinition("com.bumimobile.core", "Core", "Base tooling, settings, and utilities. Already embedded in this project.", "Packages/com.bumimobile.core", defaultVersion, true),
                    new PackageDefinition("com.bumimobile.auth", "Auth", "Firebase Auth bootstrap with optional Google Play Games sign-in.", "Packages/com.bumimobile.auth", defaultVersion, false, new List<PostInstallDependency>
                    {
                        PostInstallDependency.LocalEmbedded("com.google.play.games", "Google Play Games", "Packages/com.google.play.games")
                    }),
                    new PackageDefinition("com.bumimobile.audio", "Audio", "Audio systems, mixers, and helpers.", "Packages/com.bumimobile.audio", defaultVersion),
                    new PackageDefinition("com.bumimobile.currency", "Currency", "Currency definitions and handlers.", "Packages/com.bumimobile.currency", defaultVersion),
                    new PackageDefinition("com.bumimobile.defines", "Defines", "Shared scripting defines and configuration presets.", "Packages/com.bumimobile.defines", defaultVersion),
                    new PackageDefinition("com.bumimobile.eventcalendar", "Event Calendar", "Local-only seasonal scheduling, sprite swap runtime, and editor authoring tools.", "Packages/com.bumimobile.eventcalendar", defaultVersion),
                    new PackageDefinition("com.bumimobile.fullserializer", "FullSerializer", "Bumi-packaged FullSerializer JSON pipeline for shared use.", "Packages/com.bumimobile.fullserializer", defaultVersion),
                    new PackageDefinition("com.bumimobile.haptic", "Haptic", "Haptic feedback abstractions and presets.", "Packages/com.bumimobile.haptic", defaultVersion),
                    new PackageDefinition("com.bumimobile.leaderboard", "Leaderboard", "Firestore-backed leaderboard service with caching and warmup helpers.", "Packages/com.bumimobile.leaderboard", defaultVersion),
                    new PackageDefinition("com.bumimobile.localization", "Localization", "Localization data pipelines and helpers.", "Packages/com.bumimobile.localization", defaultVersion),
                    new PackageDefinition("com.bumimobile.nativeshare", "Native Share", "Sharing bridges for iOS/Android.", "Packages/com.bumimobile.nativeshare", defaultVersion),
                    new PackageDefinition("com.bumimobile.pool", "Pool", "Object pooling utilities.", "Packages/com.bumimobile.pool", defaultVersion),
                    new PackageDefinition("com.bumimobile.pushnotification", "Push Notification", "Push notification bridges and helpers.", "Packages/com.bumimobile.pushnotification", defaultVersion),
                    new PackageDefinition("com.bumimobile.save", "Save", "Save system entry points and persistence helpers.", "Packages/com.bumimobile.save", defaultVersion),
                    new PackageDefinition("com.bumimobile.security", "Security", "SaveCrypto encryption utilities and editor tooling.", "Packages/com.bumimobile.security", defaultVersion),
                    new PackageDefinition("com.bumimobile.skins", "Skins", "Skin/theme data structures and runtime.", "Packages/com.bumimobile.skins", defaultVersion),
                    new PackageDefinition("com.bumimobile.ui", "UI", "Common UI widgets and theming.", "Packages/com.bumimobile.ui", defaultVersion),
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

            [Serializable]
            public class PostInstallDependency
            {
                public string PackageId;
                public string Version;
                public DependencyInstallMode InstallMode;
                public string DisplayName;
                public string EmbeddedRelativePath;

                public PostInstallDependency(string packageId, string version, DependencyInstallMode installMode, string displayName = null, string embeddedRelativePath = null)
                {
                    PackageId = packageId;
                    Version = version;
                    InstallMode = installMode;
                    DisplayName = displayName;
                    EmbeddedRelativePath = embeddedRelativePath;
                }

                public static PostInstallDependency LocalTarball(string packageId, string displayName)
                {
                    return new PostInstallDependency(packageId, null, DependencyInstallMode.LocalTarball, displayName);
                }

                public static PostInstallDependency LocalEmbedded(string packageId, string displayName, string embeddedRelativePath)
                {
                    return new PostInstallDependency(packageId, null, DependencyInstallMode.LocalEmbedded, displayName, embeddedRelativePath);
                }

                public string GetIdentifier()
                {
                    if (string.IsNullOrEmpty(Version))
                    {
                        return PackageId;
                    }

                    return $"{PackageId}@{Version}";
                }

                public string GetEmbeddedPath()
                {
                    if (string.IsNullOrEmpty(EmbeddedRelativePath))
                    {
                        return null;
                    }

                    string projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
                    string fullPath = System.IO.Path.Combine(projectRoot, EmbeddedRelativePath);
                    return System.IO.Path.GetFullPath(fullPath);
                }
            }

            public enum DependencyInstallMode
            {
                Registry,
                LocalTarball,
                LocalEmbedded
            }
        }
    }
}
