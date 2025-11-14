using System.Collections.Generic;
using UnityEngine;

namespace BumiMobile
{
    [CreateAssetMenu(fileName = "Audio Clips", menuName = "Data/Core/Audio Clips")]
    public class AudioClips : ScriptableObject
    {
        [BoxGroup("UI", "UI")]
        public AudioClip buttonSound;
    }
}

// -----------------
// Audio Controller v 0.4
// -----------------