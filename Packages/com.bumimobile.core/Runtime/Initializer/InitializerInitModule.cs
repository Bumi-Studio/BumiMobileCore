using UnityEngine;

namespace BumiMobile
{
    [RegisterModule("Initializer Settings", true, order: 999)]
    public class InitializerInitModule : InitModule
    {
        public override string ModuleName => "Initializer Settings";

        [Tooltip("If manual mode is enabled, the loading screen stays active until GameLoading.MarkAsReadyToHide is called.")]
        [Header("Loading")]
        [SerializeField] bool manualControlMode;

        [Space]
        [SerializeField] GameObject systemMessagesPrefab;

        public override void CreateComponent()
        {
            if (manualControlMode)
                GameLoading.EnableManualControlMode();

            if (systemMessagesPrefab == null)
            {
                Debug.LogWarning("The System Message prefab isn't linked. This may affect the user experience while playing your game.");
                return;
            }

            if (systemMessagesPrefab.GetComponent<SystemMessage>() == null)
            {
                Debug.LogError("The Linked System Message prefab doesn't have the SystemMessage component attached to it.");
                return;
            }

            GameObject messagesCanvasObject = Object.Instantiate(systemMessagesPrefab);
            messagesCanvasObject.name = systemMessagesPrefab.name;
            messagesCanvasObject.transform.SetParent(Initializer.Transform);
        }
    }
}
