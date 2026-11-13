using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "NotificationSettings", menuName = "BumiMobile/Push Notifications/Notification Settings")]
public class NotificationSettings : ScriptableObject
{
    [Header("Notification Catalog")]
    [SerializeField]
    private NotificationTemplateCatalog templateCatalog;

    [SerializeField]
    [FormerlySerializedAs("scheduleOnInit")]
    [Tooltip("Schedule notifications when the app goes to the background and clear them when it returns.")]
    private bool scheduleOnBackground;

    [SerializeField]
    [Tooltip("Allow background scheduling while running in the Unity Editor.")]
    private bool scheduleInEditor;

    [SerializeField]
    [Tooltip("Log a warning when no catalog is provided and the fallback defaults are used.")]
    private bool logMissingCatalogWarning = true;

    [SerializeField]
    [Tooltip("When multiple notifications of the same type are scheduled at the same time, randomly select one and reschedule others for future days.")]
    private bool enableDeduplication = true;

    public NotificationTemplateCatalog TemplateCatalog => templateCatalog;
    public bool ScheduleOnBackground => scheduleOnBackground;
    public bool ScheduleInEditor => scheduleInEditor;
    public bool EnableDeduplication => enableDeduplication;

    public void ApplyCatalog()
    {
        NotificationManager.SetDeduplicationEnabled(enableDeduplication);

        if (templateCatalog != null)
        {
            NotificationManager.RegisterCatalog(templateCatalog);
            return;
        }

        NotificationManager.RegisterCatalog(null);

        if (logMissingCatalogWarning)
        {
            Debug.LogWarning("[NotificationSettings] No NotificationTemplateCatalog assigned. Falling back to the built-in defaults.");
        }
    }
}
