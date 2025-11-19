using UnityEngine;

[CreateAssetMenu(fileName = "NotificationSettings", menuName = "BumiMobile/Push Notifications/Notification Settings")]
public class NotificationSettings : ScriptableObject
{
    [Header("Notification Catalog")]
    [SerializeField]
    private NotificationTemplateCatalog templateCatalog;

    [SerializeField]
    [Tooltip("Schedule notifications automatically when the module initializes.")]
    private bool scheduleOnInit;

    [SerializeField]
    [Tooltip("Allow automatic scheduling to run while in the Unity Editor (when not in Play Mode).")]
    private bool scheduleInEditor;

    [SerializeField]
    [Tooltip("Log a warning when no catalog is provided and the fallback defaults are used.")]
    private bool logMissingCatalogWarning = true;

    public NotificationTemplateCatalog TemplateCatalog => templateCatalog;
    public bool ScheduleOnInit => scheduleOnInit;
    public bool ScheduleInEditor => scheduleInEditor;

    public void ApplyCatalog()
    {
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

    public void ScheduleIfNeeded(NotificationScheduleContext context = null)
    {
        if (!scheduleOnInit)
        {
            return;
        }

        if (!Application.isPlaying && !scheduleInEditor)
        {
            return;
        }

        NotificationManager.ScheduleAllNotifications(context);
    }
}
