using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BumiMobile
{
    /// <summary>
    /// Catalog of reusable audio clips grouped by a readable identifier.
    /// </summary>
    [CreateAssetMenu(fileName = "Audio Clips", menuName = "Data/Core/Audio Clips")]
    public class AudioClips : ScriptableObject
    {
        /// <summary>Default key used by legacy button click sounds.</summary>
        public const string ButtonSoundKey = "ui/button";

        [SerializeField]
        private List<AudioClipGroup> groups = new List<AudioClipGroup> { new AudioClipGroup("UI") };



        private Dictionary<string, AudioClipSlot> lookup;

        /// <summary>All configured clip groups as shown in the inspector.</summary>
        public IReadOnlyList<AudioClipGroup> Groups => groups;

        /// <summary>Backwards compatible accessor for the old button click field.</summary>
        public AudioClip buttonSound => GetClipOrNull(ButtonSoundKey);

        /// <summary>Try to resolve a clip by id defined in the catalog.</summary>
        public bool TryGetClip(string clipId, out AudioClip clip)
        {
            clip = GetClipOrNull(clipId);
            return clip != null;
        }

        /// <summary>Return a clip by id or null when the slot does not exist.</summary>
        public AudioClip GetClipOrNull(string clipId)
        {
            if (TryGetSlot(clipId, out var slot))
            {
                return slot.Clip;
            }

            return null;
        }

        /// <summary>Enumerate all slots for tooling/integration purposes.</summary>
        public IEnumerable<AudioClipSlot> EnumerateSlots()
        {
            EnsureLookup();
            return lookup.Values;
        }

        /// <summary>Resolve an entire slot (clip + metadata) by id.</summary>
        public bool TryGetSlot(string clipId, out AudioClipSlot slot)
        {
            if (string.IsNullOrWhiteSpace(clipId))
            {
                slot = null;
                return false;
            }

            EnsureLookup();
            return lookup.TryGetValue(clipId, out slot) && slot != null;
        }

        private void OnEnable() => EnsureLookup(force: true);

        private void OnValidate() => EnsureLookup(force: true);

        private void EnsureLookup(bool force = false)
        {
            if (!force && lookup != null)
            {
                return;
            }

            lookup = new Dictionary<string, AudioClipSlot>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in groups)
            {
                if (group == null)
                {
                    continue;
                }

                foreach (var slot in group.Clips)
                {
                    if (slot == null)
                    {
                        continue;
                    }

                    var key = slot.Id;
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    if (!lookup.ContainsKey(key))
                    {
                        lookup.Add(key, slot);
                    }
                }
            }
        }

        [Serializable]
        public class AudioClipGroup
        {
            [SerializeField]
            private string groupName = "Group";

            [SerializeField]
            private List<AudioClipSlot> clips = new List<AudioClipSlot>();

            public AudioClipGroup() { }

            public AudioClipGroup(string groupName)
            {
                this.groupName = groupName;
            }

            public string GroupName => groupName;
            public List<AudioClipSlot> Clips => clips;
        }

        [Serializable]
        public class AudioClipSlot
        {
            [SerializeField]
            [Tooltip("Unique identifier used when requesting the clip in code (e.g. 'ui/button').")]
            private string id = "ui/button";

            [SerializeField]
            private string displayName = "Button";

            [SerializeField]
            private AudioClip clip;

            public AudioClipSlot() { }

            public AudioClipSlot(string id, string displayName, AudioClip clip)
            {
                this.id = id;
                this.displayName = displayName;
                this.clip = clip;
            }

            public string Id => id;
            public string DisplayName => displayName;
            public AudioClip Clip => clip;

            public override string ToString() => string.IsNullOrEmpty(displayName) ? id : $"{displayName} ({id})";
        }
    }
}

// -----------------
// Audio Controller v 0.4
// -----------------