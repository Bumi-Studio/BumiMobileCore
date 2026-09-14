using System;
using System.Collections.Generic;
using System.Globalization;

namespace BumiMobile
{
    public enum EventCalendarScheduleResolutionStatus
    {
        None = 0,
        ActiveEvent = 1,
        Overlap = 2,
    }

    public struct EventCalendarScheduleResolution
    {
        public EventCalendarScheduleResolution(EventCalendarScheduleResolutionStatus status, EventCalendarData activeEvent, string message)
        {
            Status = status;
            ActiveEvent = activeEvent;
            Message = message ?? string.Empty;
        }

        public EventCalendarScheduleResolutionStatus Status;
        public EventCalendarData ActiveEvent;
        public string Message;
    }

    public struct EventCalendarScheduleConflict
    {
        public EventCalendarScheduleConflict(EventCalendarData firstEvent, EventCalendarData secondEvent, DateTime firstStartUtc, DateTime firstEndUtc, DateTime secondStartUtc, DateTime secondEndUtc)
        {
            FirstEvent = firstEvent;
            SecondEvent = secondEvent;
            FirstStartUtc = firstStartUtc;
            FirstEndUtc = firstEndUtc;
            SecondStartUtc = secondStartUtc;
            SecondEndUtc = secondEndUtc;
        }

        public EventCalendarData FirstEvent;
        public EventCalendarData SecondEvent;
        public DateTime FirstStartUtc;
        public DateTime FirstEndUtc;
        public DateTime SecondStartUtc;
        public DateTime SecondEndUtc;
    }

    public static class EventCalendarScheduleUtils
    {
        public const string UtcDateTimeFormat = "yyyy-MM-ddTHH:mm:ssZ";

        public static string NormalizeEventId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        public static bool TryParseUtc(string rawValue, out DateTime utcDateTime)
        {
            utcDateTime = default;
            if (string.IsNullOrWhiteSpace(rawValue))
                return false;

            if (!DateTime.TryParse(
                    rawValue,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out DateTime parsedDateTime))
            {
                return false;
            }

            utcDateTime = parsedDateTime.Kind == DateTimeKind.Utc
                ? parsedDateTime
                : DateTime.SpecifyKind(parsedDateTime.ToUniversalTime(), DateTimeKind.Utc);

            return true;
        }

        public static string FormatUtc(DateTime utcDateTime)
        {
            DateTime normalizedUtc = utcDateTime.Kind == DateTimeKind.Utc
                ? utcDateTime
                : utcDateTime.ToUniversalTime();

            return normalizedUtc.ToString(UtcDateTimeFormat, CultureInfo.InvariantCulture);
        }

        public static bool TryGetValidScheduleRange(EventCalendarData eventData, out DateTime startUtc, out DateTime endUtc)
        {
            startUtc = default;
            endUtc = default;

            if (eventData?.Schedule == null || !eventData.Schedule.UseSchedule)
                return false;

            if (!TryParseUtc(eventData.Schedule.StartUtc, out startUtc))
                return false;

            if (!TryParseUtc(eventData.Schedule.EndUtc, out endUtc))
                return false;

            return endUtc >= startUtc;
        }

        public static EventCalendarScheduleResolution ResolveScheduledEvent(EventCalendarDatabase database, DateTime nowUtc)
        {
            if (database == null)
                return new EventCalendarScheduleResolution(EventCalendarScheduleResolutionStatus.None, null, "Event Calendar database is not assigned.");

            EventCalendarData activeEvent = null;
            EventCalendarData[] databaseEvents = database.Events;
            for (int i = 0; i < databaseEvents.Length; i++)
            {
                EventCalendarData eventData = databaseEvents[i];
                if (!TryGetValidScheduleRange(eventData, out DateTime startUtc, out DateTime endUtc))
                    continue;

                if (nowUtc < startUtc || nowUtc > endUtc)
                    continue;

                if (activeEvent != null)
                {
                    string message = $"Multiple scheduled events are active at {FormatUtc(nowUtc)}: '{GetEventLabel(activeEvent)}' and '{GetEventLabel(eventData)}'. Falling back to default visuals.";
                    return new EventCalendarScheduleResolution(EventCalendarScheduleResolutionStatus.Overlap, null, message);
                }

                activeEvent = eventData;
            }

            return activeEvent == null
                ? new EventCalendarScheduleResolution(EventCalendarScheduleResolutionStatus.None, null, string.Empty)
                : new EventCalendarScheduleResolution(EventCalendarScheduleResolutionStatus.ActiveEvent, activeEvent, string.Empty);
        }

        public static List<string> CollectScheduleWarnings(EventCalendarData eventData)
        {
            List<string> warnings = new List<string>();
            if (eventData?.Schedule == null || !eventData.Schedule.UseSchedule)
                return warnings;

            string eventLabel = GetEventLabel(eventData);
            bool hasStart = TryParseUtc(eventData.Schedule.StartUtc, out DateTime startUtc);
            bool hasEnd = TryParseUtc(eventData.Schedule.EndUtc, out DateTime endUtc);

            if (!hasStart)
                warnings.Add($"{eventLabel} has an invalid Start UTC.");

            if (!hasEnd)
                warnings.Add($"{eventLabel} has an invalid End UTC.");

            if (hasStart && hasEnd && endUtc < startUtc)
                warnings.Add($"{eventLabel} ends before it starts.");

            return warnings;
        }

        public static List<EventCalendarScheduleConflict> CollectScheduleConflicts(IEnumerable<EventCalendarData> eventDataCollection)
        {
            List<EventCalendarScheduleConflict> conflicts = new List<EventCalendarScheduleConflict>();
            if (eventDataCollection == null)
                return conflicts;

            List<(EventCalendarData EventData, DateTime StartUtc, DateTime EndUtc)> ranges = new List<(EventCalendarData, DateTime, DateTime)>();
            foreach (EventCalendarData eventData in eventDataCollection)
            {
                if (!TryGetValidScheduleRange(eventData, out DateTime startUtc, out DateTime endUtc))
                    continue;

                ranges.Add((eventData, startUtc, endUtc));
            }

            for (int i = 0; i < ranges.Count; i++)
            {
                for (int j = i + 1; j < ranges.Count; j++)
                {
                    bool overlaps = ranges[i].StartUtc <= ranges[j].EndUtc &&
                                    ranges[j].StartUtc <= ranges[i].EndUtc;
                    if (!overlaps)
                        continue;

                    conflicts.Add(new EventCalendarScheduleConflict(
                        ranges[i].EventData,
                        ranges[j].EventData,
                        ranges[i].StartUtc,
                        ranges[i].EndUtc,
                        ranges[j].StartUtc,
                        ranges[j].EndUtc));
                }
            }

            return conflicts;
        }

        public static string GetEventLabel(EventCalendarData eventData)
        {
            if (eventData == null)
                return "Missing Event";

            return string.IsNullOrWhiteSpace(eventData.DisplayName)
                ? eventData.name
                : eventData.DisplayName;
        }
    }
}
