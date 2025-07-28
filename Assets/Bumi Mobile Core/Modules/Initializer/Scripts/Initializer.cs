    #pragma warning disable 0649

using UnityEngine;
using UnityEngine.EventSystems;

namespace BumiMobile
{
    [DefaultExecutionOrder(-999)]
    public class Initializer : MonoBehaviour
    {
        private static Initializer initializer;

        [SerializeField] ProjectInitSettings initSettings;
        [SerializeField] EventSystem eventSystem;

        public static GameObject GameObject { get; private set; }
        public static Transform Transform { get; private set; }

        public static ProjectInitSettings InitSettings { get; private set; }

        private bool manualActivation;

        public void Awake()
        {
            if (initializer != null) return;

            initializer = this;

            manualActivation = false;

            InitSettings = initSettings;

            GameObject = gameObject;
            Transform = transform;

#if MODULE_INPUT_SYSTEM
            eventSystem.gameObject.GetOrSetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystem.gameObject.GetOrSetComponent<StandaloneInputModule>();
#endif

            DontDestroyOnLoad(gameObject);

            initSettings.Init(this);
        }

        public void Start()
        {
            if (!manualActivation)
                LoadGame(true);
            else
            {
                LoadGame(false);
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
    }
}
