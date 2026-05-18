using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BumiMobile
{
    [CreateAssetMenu(menuName = "Audio/Audio Library (Data-Driven)", fileName = "AudioLibrary")]
    public class AudioLibrary : ScriptableObject
    {
        [Serializable]
        public class Item
        {
            [Tooltip("Unique ID within this group, e.g., Button, Back, Win, Lose")]
            public string id;
            public AudioClip clip;
        }

        [Serializable]
        public class Group
        {
            [Tooltip("Group name, e.g., UI, Game, Music")]
            public string name = "UI";
            public List<Item> items = new();
        }

        [Tooltip("Add groups (UI, Game, Music, etc.). Each group contains items with string IDs.")]
        public List<Group> groups = new();

        // Runtime lookup: group -> (id -> clip)
        private Dictionary<string, Dictionary<string, AudioClip>> _map;

        private void OnEnable()
        {
            BuildLookup();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            NormalizeAndValidate();
            BuildLookup();
        }

        private void NormalizeAndValidate()
        {
            var seen = new HashSet<string>();

            foreach (var g in groups)
            {
                if (g == null) continue;

                g.name = g.name?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(g.name)) continue;

                foreach (var item in g.items)
                {
                    if (item == null) continue;

                    item.id = item.id?.Trim() ?? "";
                    if (string.IsNullOrWhiteSpace(item.id)) continue;

                    if (!seen.Add($"{g.name}|{item.id}"))
                    {
                        Debug.LogWarning(
                            $"[AudioLibrary] Duplicate (group, id) pair: ({g.name}, {item.id}) in '{name}'. " +
                            "Last one wins at runtime — rename or remove the duplicate.",
                            this);
                    }
                }
            }
        }
#endif

        public void BuildLookup()
        {
            _map = new Dictionary<string, Dictionary<string, AudioClip>>(StringComparer.Ordinal);

            foreach (var g in groups.Where(g => g != null && !string.IsNullOrWhiteSpace(g.name)))
            {
                var groupKey = g.name.Trim();
                if (!_map.TryGetValue(groupKey, out var inner))
                {
                    inner = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
                    _map[groupKey] = inner;
                }

                foreach (var it in g.items.Where(i => i != null && !string.IsNullOrWhiteSpace(i.id) && i.clip != null))
                {
                    var idKey = it.id.Trim();
                    inner[idKey] = it.clip; // last one wins if duplicates
                }
            }
        }

        public bool TryGet(SoundKey key, out AudioClip clip)
        {
            clip = null;
            if (string.IsNullOrEmpty(key.Group) || string.IsNullOrEmpty(key.Id)) return false;
            if (_map == null) BuildLookup();
            return _map.TryGetValue(key.Group, out var inner) && inner.TryGetValue(key.Id, out clip);
        }

        public AudioClip Get(SoundKey key)
        {
            return TryGet(key, out var clip) ? clip : null;
        }

        public AudioClip Get(string group, string id)
        {
            return TryGet(new SoundKey(group, id), out var clip) ? clip : null;
        }

        public IEnumerable<string> GetGroupNames() => groups.Select(g => g?.name).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct();

        public IEnumerable<string> GetIdsInGroup(string group)
        {
            var trimmed = group?.Trim();
            var g = groups.FirstOrDefault(x => string.Equals(x?.name?.Trim() ?? "", trimmed, StringComparison.Ordinal));
            return g == null ? Enumerable.Empty<string>() : g.items.Select(i => i?.id?.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct();
        }
    }
}