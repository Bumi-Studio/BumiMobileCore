using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "NotificationTemplateCatalog", menuName = "BumiMobile/Push Notifications/Notification Catalog")]
public class NotificationTemplateCatalog : ScriptableObject
{
    public const string DefaultResourcePath = "NotificationTemplateCatalog";

    [FormerlySerializedAs("notifications")]
    [SerializeField]
    private List<NotificationTemplate> templates = new List<NotificationTemplate>();

    public IReadOnlyList<NotificationTemplate> Notifications => templates;

    public NotificationTemplate Get(NotificationType type)
    {
        for (int i = 0; i < templates.Count; i++)
        {
            var template = templates[i];
            if (template != null && template.type == type)
            {
                return template;
            }
        }

        return null;
    }

    internal void RemoveNullEntries()
    {
        templates.RemoveAll(template => template == null);
    }

    internal void SetRuntimeTemplates(List<NotificationTemplate> runtimeTemplates)
    {
        templates = runtimeTemplates ?? new List<NotificationTemplate>();
    }
}
