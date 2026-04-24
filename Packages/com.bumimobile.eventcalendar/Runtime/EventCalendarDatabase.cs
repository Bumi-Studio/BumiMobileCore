using System;
using UnityEngine;

namespace BumiMobile
{
    [CreateAssetMenu(fileName = "EventCalendarDatabase", menuName = "Data/Event Calendar Database")]
    public class EventCalendarDatabase : ScriptableObject
    {
        [SerializeField] private EventCalendarData[] events = Array.Empty<EventCalendarData>();
        public EventCalendarData[] Events => events ?? Array.Empty<EventCalendarData>();

        public bool TryGetEventById(string eventId, out EventCalendarData eventData)
        {
            eventData = null;
            if (string.IsNullOrWhiteSpace(eventId))
                return false;

            EventCalendarData[] eventList = Events;
            for (int i = 0; i < eventList.Length; i++)
            {
                EventCalendarData currentEvent = eventList[i];
                if (currentEvent == null)
                    continue;

                if (!string.Equals(currentEvent.EventId, eventId, StringComparison.OrdinalIgnoreCase))
                    continue;

                eventData = currentEvent;
                return true;
            }

            return false;
        }

        public EventCalendarData GetEventById(string eventId)
        {
            return TryGetEventById(eventId, out EventCalendarData eventData) ? eventData : null;
        }
    }
}
