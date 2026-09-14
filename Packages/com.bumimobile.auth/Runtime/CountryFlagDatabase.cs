// Scripts/Auth/CountryFlagDatabase.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BumiMobile
{
    [CreateAssetMenu(menuName = "Data/Country Flag Database", fileName = "CountryFlagDatabase")]
    public class CountryFlagDatabase : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [Tooltip("ISO 3166-1 alpha-2 (ID, US, JP, ...)")]
            public string iso2;
            [Tooltip("Nama negara (opsional). Kosongkan untuk auto (RegionInfo).")]
            public string displayName;
            [Tooltip("Sprite bendera untuk ISO tsb.")]
            public Sprite flag;
        }

        [SerializeField] List<Entry> entries = new List<Entry>();
        Dictionary<string, Entry> map;

        void OnEnable() => BuildMap();
#if UNITY_EDITOR
        void OnValidate() => BuildMap();
#endif

        void BuildMap()
        {
            map = new Dictionary<string, Entry>(entries.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var e in entries)
            {
                if (string.IsNullOrWhiteSpace(e.iso2)) continue;
                string key = e.iso2.Trim().ToUpperInvariant();
                if (!map.ContainsKey(key)) map.Add(key, e);
            }
        }

        public bool TryGet(string iso2, out Entry entry)
        {
            if (map == null) BuildMap();
            if (string.IsNullOrWhiteSpace(iso2)) { entry = default; return false; }
            return map.TryGetValue(iso2.Trim().ToUpperInvariant(), out entry);
        }

        public Sprite GetSprite(string iso2) => TryGet(iso2, out var e) ? e.flag : null;

        public string GetDisplayName(string iso2)
        {
            if (TryGet(iso2, out var e) && !string.IsNullOrWhiteSpace(e.displayName))
                return e.displayName;

            return string.IsNullOrWhiteSpace(iso2) ? "Unknown" : iso2.Trim().ToUpperInvariant();
        }
    }
}
