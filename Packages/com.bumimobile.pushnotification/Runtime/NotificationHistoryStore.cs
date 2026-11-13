using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct NotificationHistoryEntry
{
    public NotificationHistoryEntry(NotificationType type, DateTime time)
    {
        Type = type;
        Time = time;
    }

    public NotificationType Type { get; }
    public DateTime Time { get; }
}

/// <summary>
/// Persists scheduled fire times in PlayerPrefs. Entries whose fire time has passed count as
/// delivered; future entries were cancelled when the player returned and are dropped on load.
/// </summary>
internal static class NotificationHistoryStore
{
    private const string PrefsKey = "BumiMobile.PushNotification.History";
    private const int RetentionDays = 30;

    [Serializable]
    private class Entry
    {
        public int type;
        public long ticks;
    }

    [Serializable]
    private class Data
    {
        public List<Entry> entries = new List<Entry>();
    }

    public static List<NotificationHistoryEntry> LoadDelivered(DateTime now)
    {
        var result = new List<NotificationHistoryEntry>();
        var json = PlayerPrefs.GetString(PrefsKey, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            return result;
        }

        var data = JsonUtility.FromJson<Data>(json);
        if (data?.entries == null)
        {
            return result;
        }

        var oldest = now.AddDays(-RetentionDays);
        foreach (var entry in data.entries)
        {
            if (!Enum.IsDefined(typeof(NotificationType), entry.type))
            {
                continue;
            }

            var time = new DateTime(entry.ticks);
            if (time <= now && time > oldest)
            {
                result.Add(new NotificationHistoryEntry((NotificationType)entry.type, time));
            }
        }

        return result;
    }

    public static void Save(IEnumerable<NotificationHistoryEntry> entries)
    {
        var data = new Data();
        foreach (var entry in entries)
        {
            data.entries.Add(new Entry { type = (int)entry.Type, ticks = entry.Time.Ticks });
        }

        PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(data));
        // Scheduling runs as the app is suspended; the process may be killed before Unity flushes.
        PlayerPrefs.Save();
    }
}
