#pragma warning disable 0649

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BumiMobile
{
    [DefaultExecutionOrder(-999)]
    public class Initializer : MonoBehaviour
    {
        private static Initializer instance;

        [SerializeField] ProjectInitSettings initSettings;
        [SerializeField] EventSystem eventSystem;
        [SerializeField] bool useEventSystem = true;

        public static GameObject GameObject { get; private set; }
        public static Transform Transform { get; private set; }
        public static ProjectInitSettings InitSettings { get; private set; }
        public static float InitializationProgress { get; private set; }
        public static string InitializationMessage { get; private set; }
        public static bool ModulesReady { get; private set; }
        public static event Action<float, string> OnInitializationProgress;

        private bool manualActivation;
        private InitModule[] configuredModules = Array.Empty<InitModule>();
        private Coroutine moduleInitializationRoutine;
        private bool loadingTaskRegistered;

        private void Awake()
        {
            if (instance != null) return;

            instance = this;
            manualActivation = false;

            InitSettings = initSettings;

            GameObject = gameObject;
            Transform = transform;
            InitializerContext.Set(GameObject, Transform);

            InitializationProgress = 0f;
            InitializationMessage = string.Empty;
            ModulesReady = false;

            if (useEventSystem && eventSystem != null)
            {
                if (!eventSystem.gameObject.activeSelf)
                {
                    eventSystem.gameObject.SetActive(true);
                }

                if (!eventSystem.enabled)
                {
                    eventSystem.enabled = true;
                }

#if MODULE_INPUT_SYSTEM
                eventSystem.gameObject.GetOrSetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                eventSystem.gameObject.GetOrSetComponent<StandaloneInputModule>();
#endif
            }
            else if (useEventSystem)
            {
                Debug.LogWarning("[Initializer] EventSystem is enabled but no reference was assigned.");
            }
            else if (!useEventSystem && eventSystem != null)
            {
#if MODULE_INPUT_SYSTEM
                var inputModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                if (inputModule != null)
                {
                    Destroy(inputModule);
                }
#endif
                var standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
                if (standaloneModule != null)
                {
                    Destroy(standaloneModule);
                }

                eventSystem.enabled = false;
                eventSystem.gameObject.SetActive(false);
            }

            DontDestroyOnLoad(gameObject);

            if (initSettings != null)
            {
                initSettings.Init(this);
            }
            else
            {
                configuredModules = Array.Empty<InitModule>();
            }

            RegisterInitializationTask();
        }

        private void Start()
        {
            if (moduleInitializationRoutine == null)
            {
                moduleInitializationRoutine = StartCoroutine(InitializeModulesRoutine());
            }

            if (!manualActivation)
            {
                LoadGame(true);
            }
        }

        public void LoadGame(bool loadingScene)
        {
            if (loadingScene)
            {
                GameLoading.LoadGameScene();
            }
        }

        public void EnableManualActivation()
        {
            manualActivation = true;
        }

        internal void ConfigureModules(InitModule[] modules)
        {
            configuredModules = modules ?? Array.Empty<InitModule>();
        }

        public static void SetInitializationStatus(string message)
        {
            if (instance == null)
                return;

            instance.UpdateInitializationProgress(InitializationProgress, message);
        }

        public static void ReportInitializationProgress(float progress, string message)
        {
            if (instance == null)
                return;

            instance.UpdateInitializationProgress(progress, message);
        }

        private IEnumerator InitializeModulesRoutine()
        {
            var modules = configuredModules ?? Array.Empty<InitModule>();
            int totalModules = modules.Length;

            if (totalModules == 0)
            {
                ModulesReady = true;
                UpdateInitializationProgress(1f, "Initialization complete");
                moduleInitializationRoutine = null;
                yield break;
            }

            for (int i = 0; i < totalModules; i++)
            {
                var module = modules[i];
                string moduleName = GetModuleName(module, i);
                float startProgress = (float)i / totalModules;
                UpdateInitializationProgress(startProgress, $"Initializing {moduleName}...");

                if (module == null)
                {
                    continue;
                }

                try
                {
                    module.CreateComponent();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Initializer] Failed to create module {moduleName}: {e}");
                    continue;
                }

                if (module.IsAsync)
                {
                    IEnumerator routine = null;
                    try
                    {
                        routine = module.InitializeCoroutine(this);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[Initializer] {moduleName} coroutine threw: {e}");
                    }

                    if (routine != null)
                    {
                        yield return RunModuleRoutine(routine, moduleName);
                    }
                }

                float endProgress = (float)(i + 1) / totalModules;
                UpdateInitializationProgress(endProgress, $"{moduleName} ready");
            }

            ModulesReady = true;
            UpdateInitializationProgress(1f, "Initialization complete");
            moduleInitializationRoutine = null;
        }

        private IEnumerator RunModuleRoutine(IEnumerator routine, string moduleName)
        {
            while (true)
            {
                bool moveNext;
                try
                {
                    moveNext = routine.MoveNext();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Initializer] {moduleName} coroutine failed: {e}");
                    yield break;
                }

                if (!moveNext)
                    yield break;

                yield return routine.Current;
            }
        }

        private void UpdateInitializationProgress(float progress, string message)
        {
            InitializationProgress = Mathf.Clamp01(progress);
            InitializationMessage = message;
            OnInitializationProgress?.Invoke(InitializationProgress, InitializationMessage);
            GameLoading.SetLoadingMessage(message);
        }

        private static string GetModuleName(InitModule module, int index)
        {
            if (module == null)
            {
                return $"Module {index}";
            }

            var componentName = module.GetType().Name;
            return string.IsNullOrEmpty(componentName) ? $"Module {index}" : componentName;
        }

        public static Coroutine RunCoroutine(IEnumerator routine)
        {
            if (instance == null)
            {
                Debug.LogError("[Initializer]: Unable to run coroutine before Initializer is ready.");
                return null;
            }

            if (routine == null)
            {
                Debug.LogError("[Initializer]: Coroutine routine is null.");
                return null;
            }

            return instance.StartCoroutine(routine);
        }

        public static void StopCoroutineSafe(Coroutine routine)
        {
            if (instance == null || routine == null)
                return;

            instance.StopCoroutine(routine);
        }

        private void RegisterInitializationTask()
        {
            if (loadingTaskRegistered)
                return;

            loadingTaskRegistered = true;
            GameLoading.AddTask(new ModuleInitializationTask(this));
        }

        private sealed class ModuleInitializationTask : LoadingTask
        {
            private readonly Initializer initializer;

            public ModuleInitializationTask(Initializer initializer)
            {
                this.initializer = initializer;
            }

            public override string TaskName => "Core Systems";

            protected override void OnTaskActivated()
            {
                if (initializer == null)
                {
                    CompleteTask(CompleteStatus.Skipped);
                    return;
                }

                initializer.StartCoroutine(WaitForModules());
            }

            private IEnumerator WaitForModules()
            {
                while (!Initializer.ModulesReady)
                {
                    yield return null;
                }

                CompleteTask(CompleteStatus.Completed);
            }
        }
    }
}
