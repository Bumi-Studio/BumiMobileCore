using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BumiMobile
{
    public enum EventCosmeticPostEventMode
    {
        AlwaysShow = 0,
        Lock = 1,
    }

    public enum EventCalendarEventPhase
    {
        None = 0,
        Upcoming = 1,
        Active = 2,
        Ended = 3,
        Missing = 4,
        Invalid = 5,
    }

    public enum EventCosmeticAvailability
    {
        Hidden = 0,
        Available = 1,
        LockedEventOnly = 2,
    }

    public readonly struct EventCalendarEventQuery
    {
        public EventCalendarEventQuery(EventCalendarEventPhase phase, EventCalendarData eventData, string message)
        {
            Phase = phase;
            EventData = eventData;
            Message = message ?? string.Empty;
        }

        public EventCalendarEventPhase Phase { get; }
        public EventCalendarData EventData { get; }
        public string Message { get; }
    }

    public static class EventCosmeticAvailabilityResolver
    {
        private static readonly HashSet<string> LoggedWarnings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static EventCosmeticAvailability Resolve(string cosmeticLabel, string eventId, EventCosmeticPostEventMode postEventMode, bool isOwned)
        {
            if (string.IsNullOrWhiteSpace(eventId))
                return EventCosmeticAvailability.Available;

            EventCalendarEventQuery query = EventCalendarService.GetEventQuery(eventId);
            switch (query.Phase)
            {
                case EventCalendarEventPhase.Active:
                    return EventCosmeticAvailability.Available;

                case EventCalendarEventPhase.Ended:
                    if (isOwned || postEventMode == EventCosmeticPostEventMode.AlwaysShow)
                        return EventCosmeticAvailability.Available;

                    return EventCosmeticAvailability.LockedEventOnly;

                case EventCalendarEventPhase.Upcoming:
                    return EventCosmeticAvailability.Hidden;

                case EventCalendarEventPhase.Invalid:
                case EventCalendarEventPhase.Missing:
                    LogInvalidEventWarningOnce(cosmeticLabel, eventId, query.Message);
                    return EventCosmeticAvailability.Hidden;

                default:
                    return EventCosmeticAvailability.Hidden;
            }
        }

        private static void LogInvalidEventWarningOnce(string cosmeticLabel, string eventId, string message)
        {
            string normalizedEventId = EventCalendarScheduleUtils.NormalizeEventId(eventId);
            string label = string.IsNullOrWhiteSpace(cosmeticLabel) ? "Unnamed Cosmetic" : cosmeticLabel.Trim();
            string warningKey = $"{label}|{normalizedEventId}";
            if (!LoggedWarnings.Add(warningKey))
                return;

            string reason = string.IsNullOrWhiteSpace(message) ? "Unknown event calendar issue." : message;
            Debug.LogWarning($"[Event Cosmetic] '{label}' references event '{normalizedEventId}', but it is unavailable. Hiding item. {reason}");
        }
    }

    [StaticUnload]
    public static class EventCalendarService
    {
        private static readonly HashSet<EventSpriteSwapController> Consumers = new HashSet<EventSpriteSwapController>();
        private static readonly List<EventSpriteSwapController> InvalidConsumers = new List<EventSpriteSwapController>();

        private static EventCalendarRuntimeState currentState = EventCalendarRuntimeState.NoEvent;
        private static EventCalendarDatabase eventCalendarDatabase;
        private static bool enableLocalDebugOverride;
        private static string forceEventId;
        private static bool hasCompletedInitialization;

        public static EventCalendarRuntimeState CurrentState => currentState;
        public static bool IsInitialized => hasCompletedInitialization;

        public static void Initialize(EventCalendarDatabase database, bool allowLocalDebugOverride, string forcedEventId)
        {
            eventCalendarDatabase = database;
            enableLocalDebugOverride = allowLocalDebugOverride;
            forceEventId = forcedEventId;
            hasCompletedInitialization = false;
            currentState = EventCalendarRuntimeState.NoEvent;

            ResolveAndApplyCurrentState();

            hasCompletedInitialization = true;
            NotifyConsumers(EventCalendarVisualUpdateKind.StartupVisual);
            NotifyConsumers(EventCalendarVisualUpdateKind.RuntimeState);
        }

        public static IEnumerator InitializeForInitCoroutine(EventCalendarDatabase database, bool allowLocalDebugOverride, string forcedEventId)
        {
            Initialize(database, allowLocalDebugOverride, forcedEventId);
            yield break;
        }

        public static void Register(EventSpriteSwapController controller)
        {
            if (controller == null)
                return;

            Consumers.Add(controller);

            if (hasCompletedInitialization)
            {
                EventCalendarVisualUpdateKind initialUpdateKind = controller.UsesStartupVisualFreeze
                    ? EventCalendarVisualUpdateKind.StartupVisual
                    : EventCalendarVisualUpdateKind.RuntimeState;
                controller.Apply(currentState, initialUpdateKind);
            }
        }

        public static void Unregister(EventSpriteSwapController controller)
        {
            if (controller == null)
                return;

            Consumers.Remove(controller);
        }

        public static bool TryGetActiveEventData(out EventCalendarData eventData)
        {
            eventData = currentState.EventData;
            return currentState.IsEventActive && eventData != null;
        }

        public static bool TryGetActiveEventSprite(string targetId, out Sprite sprite)
        {
            return currentState.TryGetSprite(targetId, out sprite);
        }

        public static Sprite GetEventSpriteOrDefault(string targetId, Sprite defaultSprite = null)
        {
            return TryGetActiveEventSprite(targetId, out Sprite sprite) && sprite != null ? sprite : defaultSprite;
        }

        public static EventCalendarEventQuery GetEventQuery(string rawEventId)
        {
            string eventId = EventCalendarScheduleUtils.NormalizeEventId(rawEventId);
            if (string.IsNullOrEmpty(eventId))
                return new EventCalendarEventQuery(EventCalendarEventPhase.None, null, string.Empty);

            EnsureDatabaseResolved();
            if (eventCalendarDatabase == null)
                return new EventCalendarEventQuery(EventCalendarEventPhase.Missing, null, "Event Calendar database is not assigned.");

            if (!eventCalendarDatabase.TryGetEventById(eventId, out EventCalendarData eventData) || eventData == null)
                return new EventCalendarEventQuery(EventCalendarEventPhase.Missing, null, $"Missing local event definition for '{eventId}'.");

            string forcedEventId = GetForcedEventId();
            if (!string.IsNullOrEmpty(forcedEventId) && string.Equals(forcedEventId, eventId, StringComparison.OrdinalIgnoreCase))
                return new EventCalendarEventQuery(EventCalendarEventPhase.Active, eventData, "Local debug override is active.");

            if (!EventCalendarScheduleUtils.TryGetValidScheduleRange(eventData, out DateTime startUtc, out DateTime endUtc))
                return new EventCalendarEventQuery(EventCalendarEventPhase.Invalid, eventData, $"Event '{eventId}' does not have a valid schedule range.");

            DateTime nowUtc = DateTime.UtcNow;
            if (nowUtc < startUtc)
                return new EventCalendarEventQuery(EventCalendarEventPhase.Upcoming, eventData, string.Empty);

            if (nowUtc > endUtc)
                return new EventCalendarEventQuery(EventCalendarEventPhase.Ended, eventData, string.Empty);

            return new EventCalendarEventQuery(EventCalendarEventPhase.Active, eventData, string.Empty);
        }

        private static void ResolveAndApplyCurrentState()
        {
            EnsureDatabaseResolved();
            if (eventCalendarDatabase == null)
            {
                currentState = EventCalendarRuntimeState.NoEvent;
                return;
            }

            string debugEventId = GetForcedEventId();
            if (!string.IsNullOrEmpty(debugEventId))
            {
                if (TryBuildState(debugEventId, out EventCalendarRuntimeState debugState))
                {
                    currentState = debugState;
                    return;
                }

                currentState = EventCalendarRuntimeState.NoEvent;
                return;
            }

            EventCalendarScheduleResolution resolution = EventCalendarScheduleUtils.ResolveScheduledEvent(eventCalendarDatabase, DateTime.UtcNow);
            if (resolution.Status == EventCalendarScheduleResolutionStatus.Overlap)
            {
                Debug.LogWarning($"[Event Calendar] {resolution.Message}");
                currentState = EventCalendarRuntimeState.NoEvent;
                return;
            }

            if (resolution.Status != EventCalendarScheduleResolutionStatus.ActiveEvent || resolution.ActiveEvent == null)
            {
                currentState = EventCalendarRuntimeState.NoEvent;
                return;
            }

            if (!TryBuildState(resolution.ActiveEvent.EventId, out EventCalendarRuntimeState runtimeState))
            {
                currentState = EventCalendarRuntimeState.NoEvent;
                return;
            }

            currentState = runtimeState;
        }

        private static bool TryBuildState(string rawEventId, out EventCalendarRuntimeState state)
        {
            state = EventCalendarRuntimeState.NoEvent;

            string eventId = EventCalendarScheduleUtils.NormalizeEventId(rawEventId);
            if (string.IsNullOrEmpty(eventId))
                return false;

            EnsureDatabaseResolved();
            if (eventCalendarDatabase == null)
                return false;

            if (!eventCalendarDatabase.TryGetEventById(eventId, out EventCalendarData eventData) || eventData == null)
            {
                Debug.LogWarning($"[Event Calendar] Missing local event definition for '{eventId}'.");
                return false;
            }

            if (!eventData.HasValidSpriteLibraryReference)
            {
                Debug.LogWarning($"[Event Calendar] Event '{eventId}' does not have a valid sprite library reference.");
                return false;
            }

            Dictionary<string, Sprite> spriteLookup = BuildSpriteLookup(eventData);
            state = new EventCalendarRuntimeState(
                true,
                eventId,
                eventData,
                spriteLookup);

            return true;
        }

        private static Dictionary<string, Sprite> BuildSpriteLookup(EventCalendarData eventData)
        {
            Dictionary<string, Sprite> spriteLookup = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
            EventSpriteLibrary spriteLibrary = eventData != null ? eventData.SpriteLibrary : null;
            if (spriteLibrary == null)
                return spriteLookup;

            EventSpriteLibraryEntry[] entries = spriteLibrary.Entries;
            for (int i = 0; i < entries.Length; i++)
            {
                EventSpriteLibraryEntry entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.TargetId) || entry.Sprite == null)
                    continue;

                if (spriteLookup.ContainsKey(entry.TargetId))
                {
                    Debug.LogWarning($"[Event Calendar] Duplicate target id '{entry.TargetId}' in '{eventData.DisplayName}'.");
                    continue;
                }

                spriteLookup.Add(entry.TargetId, entry.Sprite);
            }

            return spriteLookup;
        }

        private static void NotifyConsumers(EventCalendarVisualUpdateKind updateKind)
        {
            InvalidConsumers.Clear();

            foreach (EventSpriteSwapController consumer in Consumers)
            {
                if (consumer == null)
                {
                    InvalidConsumers.Add(consumer);
                    continue;
                }

                consumer.Apply(currentState, updateKind);
            }

            for (int i = 0; i < InvalidConsumers.Count; i++)
            {
                Consumers.Remove(InvalidConsumers[i]);
            }
        }

        private static EventCalendarDatabase ResolveDatabaseFromInitSettings()
        {
            if (Initializer.InitSettings == null)
                return null;

            EventCalendarInitModule module = Initializer.InitSettings.GetModule<EventCalendarInitModule>();
            return module != null ? module.EventCalendarDatabase : null;
        }

        private static void EnsureDatabaseResolved()
        {
            if (eventCalendarDatabase == null)
                eventCalendarDatabase = ResolveDatabaseFromInitSettings();
        }

        private static string GetForcedEventId()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!enableLocalDebugOverride)
                return null;

            return EventCalendarScheduleUtils.NormalizeEventId(forceEventId);
#else
            return null;
#endif
        }

        private static void UnloadStatic()
        {
            Consumers.Clear();
            InvalidConsumers.Clear();
            currentState = EventCalendarRuntimeState.NoEvent;
            eventCalendarDatabase = null;
            enableLocalDebugOverride = false;
            forceEventId = null;
            hasCompletedInitialization = false;
        }
    }

    public sealed class EventCalendarRuntimeState
    {
        private static readonly IReadOnlyDictionary<string, Sprite> EmptySprites = new Dictionary<string, Sprite>(0);

        public static readonly EventCalendarRuntimeState NoEvent = new EventCalendarRuntimeState(
            false,
            string.Empty,
            null,
            EmptySprites);

        public EventCalendarRuntimeState(bool isEventActive, string eventId, EventCalendarData eventData, IReadOnlyDictionary<string, Sprite> sprites)
        {
            IsEventActive = isEventActive;
            EventId = string.IsNullOrWhiteSpace(eventId) ? string.Empty : eventId;
            EventData = eventData;
            Sprites = sprites ?? EmptySprites;
        }

        public bool IsEventActive { get; }
        public string EventId { get; }
        public EventCalendarData EventData { get; }
        public IReadOnlyDictionary<string, Sprite> Sprites { get; }

        public bool TryGetSprite(string targetId, out Sprite sprite)
        {
            sprite = null;
            if (!IsEventActive || string.IsNullOrWhiteSpace(targetId) || Sprites == null)
                return false;

            return Sprites.TryGetValue(targetId, out sprite);
        }
    }
}
