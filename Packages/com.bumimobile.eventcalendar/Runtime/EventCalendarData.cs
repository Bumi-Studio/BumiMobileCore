using System;
using UnityEngine;

namespace BumiMobile
{
    [CreateAssetMenu(fileName = "EventCalendarData", menuName = "Data/Event Calendar Data")]
    public class EventCalendarData : ScriptableObject
    {
        [SerializeField] private string eventId;
        public string EventId => string.IsNullOrWhiteSpace(eventId) ? string.Empty : eventId.Trim();

        [SerializeField] private string displayName;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName.Trim();

        [SerializeField] private EventSpriteLibrary spriteLibrary;
        public EventSpriteLibrary SpriteLibrary => spriteLibrary;

        [SerializeField] private EventCalendarSchedule schedule = new EventCalendarSchedule();
        public EventCalendarSchedule Schedule => schedule ??= new EventCalendarSchedule();

        public bool HasValidSpriteLibraryReference => spriteLibrary != null;
    }

    [Serializable]
    public sealed class EventCalendarSchedule
    {
        [SerializeField] private bool useSchedule;
        public bool UseSchedule => useSchedule;

        [SerializeField] private string startUtc = string.Empty;
        public string StartUtc => string.IsNullOrWhiteSpace(startUtc) ? string.Empty : startUtc.Trim();

        [SerializeField] private string endUtc = string.Empty;
        public string EndUtc => string.IsNullOrWhiteSpace(endUtc) ? string.Empty : endUtc.Trim();
    }
}
