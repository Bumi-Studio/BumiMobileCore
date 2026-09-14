using System;
using UnityEngine;

namespace BumiMobile
{
    [CreateAssetMenu(fileName = "EventSpriteLibrary", menuName = "Data/Event Sprite Library")]
    public class EventSpriteLibrary : ScriptableObject
    {
        [SerializeField] private EventSpriteLibraryEntry[] entries = Array.Empty<EventSpriteLibraryEntry>();
        public EventSpriteLibraryEntry[] Entries => entries ?? Array.Empty<EventSpriteLibraryEntry>();
    }

    [Serializable]
    public class EventSpriteLibraryEntry
    {
        [SerializeField] private string targetId;
        public string TargetId => string.IsNullOrWhiteSpace(targetId) ? string.Empty : targetId.Trim();

        [SerializeField] private Sprite sprite;
        public Sprite Sprite => sprite;
    }
}
