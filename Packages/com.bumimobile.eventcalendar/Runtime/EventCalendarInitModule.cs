#pragma warning disable 0649

using System.Collections;
using UnityEngine;

namespace BumiMobile
{
    [RegisterModule("Event Calendar", core: false, order: 5)]
    public class EventCalendarInitModule : InitModule
    {
        public override string ModuleName => "Event Calendar";

        [SerializeField] private EventCalendarDatabase eventCalendarDatabase;
        public EventCalendarDatabase EventCalendarDatabase => eventCalendarDatabase;

        [SerializeField] private bool enableLocalDebugOverride = true;

        [SerializeField] private string forceEventId;

        public override bool IsAsync => true;

        public override void CreateComponent()
        {
            EventCalendarService.Initialize(eventCalendarDatabase, enableLocalDebugOverride, forceEventId);
        }

        public override IEnumerator InitializeCoroutine(Initializer initializer)
        {
            return EventCalendarService.InitializeForInitCoroutine(eventCalendarDatabase, enableLocalDebugOverride, forceEventId);
        }
    }
}
