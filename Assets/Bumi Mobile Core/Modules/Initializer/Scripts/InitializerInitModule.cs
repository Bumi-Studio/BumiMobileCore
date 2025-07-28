using UnityEngine;

namespace BumiMobile
{
    [RegisterModule("Initializer Settings", true, order: 999)]
    public class InitializerInitModule : InitModule
    {
        public override string ModuleName => "Initializer Settings";

        [Tooltip("If manual mode is enabled, the loading screen will be active until GameLoading.MarkAsReadyToHide method has been called.")]
        [Header("Loading")]
        [SerializeField] bool manualControlMode;

        [Space]
        [SerializeField] GameObject systemMessagesPrefab;

        public override void CreateComponent()
        {
            if (manualControlMode)
                GameLoading.EnableManualControlMode();

            if(systemMessagesPrefab != null)
            {
                if(systemMessagesPrefab.GetComponent<SystemMessage>() != null)
                {
                    GameObject messagesCanvasObject = Instantiate(systemMessagesPrefab);
                    messagesCanvasObject.name = systemMessagesPrefab.name;
                    messagesCanvasObject.transform.SetParent(Initializer.Transform);
                }
                else
                {
                    Debug.LogError("The Linked System Message prefab doesn't have the SystemMessage component attached to it.");
                }
            }
            else
            {
                Debug.LogWarning("The System Message prefab isn't linked. This may affect the user experience while playing your game.");
            }
        }
    }
}
