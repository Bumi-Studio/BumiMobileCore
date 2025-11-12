#pragma warning disable 0649

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

        public static GameObject GameObject { get; private set; }
        public static Transform Transform { get; private set; }
        public static ProjectInitSettings InitSettings { get; private set; }

        private bool manualActivation;

        private void Awake()
        {
            if (instance != null) return;

            instance = this;
            manualActivation = false;

            InitSettings = initSettings;

            GameObject = gameObject;
            Transform = transform;
            InitializerContext.Set(GameObject, Transform);

#if MODULE_INPUT_SYSTEM
            eventSystem.gameObject.GetOrSetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystem.gameObject.GetOrSetComponent<StandaloneInputModule>();
#endif

            DontDestroyOnLoad(gameObject);
            initSettings.Init(this);
        }

        private void Start()
        {
            if (!manualActivation)
                LoadGame(true);
            else
                LoadGame(false);
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

    }
}
