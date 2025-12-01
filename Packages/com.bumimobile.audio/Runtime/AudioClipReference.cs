using System;
using UnityEngine;

namespace BumiMobile
{
    /// <summary>
    /// Lightweight reference that stores a clip id and resolves it against a catalog at runtime.
    /// </summary>
    [Serializable]
    public struct AudioClipReference
    {
        [SerializeField]
        private AudioClips catalog;

        [SerializeField]
        [Tooltip("Identifier defined inside the AudioClips catalog (e.g. 'ui/button').")]
        private string clipId;

        public string ClipId => clipId;
        public AudioClips Catalog => catalog;

        public AudioClip Resolve(AudioClips overrideCatalog = null)
        {
            AudioClips source = overrideCatalog ?? catalog ?? AudioController.AudioClips;
            return source != null ? source.GetClipOrNull(clipId) : null;
        }

        public bool TryResolve(out AudioClip clip, AudioClips overrideCatalog = null)
        {
            clip = Resolve(overrideCatalog);
            return clip != null;
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(clipId) ? base.ToString() : clipId;
        }
    }
}
