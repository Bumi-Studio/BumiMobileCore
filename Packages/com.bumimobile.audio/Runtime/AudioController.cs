using System.Collections.Generic;
using UnityEngine;

namespace BumiMobile
{
    [StaticUnload]
    public static class AudioController
    {
        private static List<PooledSource> audioSourcesPool;
        private static Transform _poolRoot;
        private static int _maxPoolSize = 64;

        private static AudioLibrary audioLibrary;
        public static AudioLibrary AudioLibrary => audioLibrary;

        private static AudioListener audioListener;
        public static AudioListener AudioListener => audioListener;

        // Default 3D audio settings
        private static float maxDistance = 30;
        private static float spread = 180;
        private static AnimationCurve rolloffCurve = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(1.0f, 0.0f));

        public static OnVolumeChangedCallback VolumeChanged;

        private static Dictionary<AudioType, float> volumeDictionary;

        public static void Init(AudioLibrary audioLibrary, int audioSourcesPoolSize, int maxPoolSize = 64)
        {
            if (audioLibrary == null)
            {
                Debug.LogError("[AudioController]: Audio Library is NULL! Please assign an AudioLibrary asset on the Audio Controller module.");
                return;
            }

            _maxPoolSize = Mathf.Max(audioSourcesPoolSize, maxPoolSize);

            volumeDictionary = new Dictionary<AudioType, float>();
            // Create audio listener
            CreateAudioListener();

            AudioController.audioLibrary = audioLibrary;

            // Create pool root to keep scene hierarchy clean
            if (_poolRoot == null)
            {
                var root = new GameObject("[AUDIO POOL]");
                GameObject.DontDestroyOnLoad(root);
                _poolRoot = root.transform;
            }

            // Create audio source objects
            audioSourcesPool = new List<PooledSource>();
            for (int i = 0; i < audioSourcesPoolSize; i++)
            {
                audioSourcesPool.Add(new PooledSource());
            }
        }

        public static void OverrideDefault3DAudioSettings(float maxDistance, float spread, AnimationCurve rolloffCurve)
        {
            AudioController.maxDistance = maxDistance;
            AudioController.spread = spread;
            AudioController.rolloffCurve = rolloffCurve;
        }

        public static void ApplyDefaultSettings(ref AudioSource audioSource)
        {
            audioSource.maxDistance = maxDistance;
            audioSource.spread = spread;
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, rolloffCurve);
        }

        private static void CreateAudioListener()
        {
            if (audioListener != null)
                return;

            // Create game object for listener
            GameObject listenerObject = new GameObject("[AUDIO LISTENER]");
            listenerObject.transform.position = Vector3.zero;

            // Mark as non-destroyable
            GameObject.DontDestroyOnLoad(listenerObject);

            // Add listener component to created object
            audioListener = listenerObject.AddComponent<AudioListener>();
        }

        public static Transform AttachAudioListener(Transform parentObject)
        {
            if (audioListener == null)
                CreateAudioListener();

            Transform audioListenerTransform = audioListener.transform;
            audioListenerTransform.SetParent(parentObject);
            audioListenerTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            return audioListenerTransform;
        }

        public static void ResetAudioLisenerParent()
        {
            if (audioListener == null) return;

            audioListener.transform.SetParent(null);

            GameObject.DontDestroyOnLoad(audioListener.gameObject);
        }

        /// <summary>
        /// Stop all active streams
        /// </summary>
        public static void ReleaseSources()
        {
            foreach (PooledSource source in audioSourcesPool)
            {
                if (source.IsPlaying)
                {
                    source.AudioSource.Stop();
                }
            }
        }

        public static void PlaySound(AudioClip clip, float volumePercentage = 1.0f, float pitch = 1.0f)
        {
            if (clip == null)
            {
                Debug.LogError("[AudioController]: Audio clip is null");
                return;
            }

            PooledSource source = GetAudioSource();

            AudioSource audioSource = source.AudioSource;
            audioSource.spatialBlend = 0.0f; // 2D sound
            audioSource.pitch = pitch;

            source.Play(clip, volumePercentage, AudioType.Sound);
        }

        public static void PlaySound(AudioClip clip, Vector3 position, float volumePercentage = 1.0f, float pitch = 1.0f)
        {
            if (clip == null)
            {
                Debug.LogError("[AudioController]: Audio clip is null");
                return;
            }

            PooledSource source = GetAudioSource();

            AudioSource audioSource = source.AudioSource;
            audioSource.transform.position = position;
            audioSource.spatialBlend = 1.0f; // 3D sound
            audioSource.pitch = pitch;

            source.Play(clip, volumePercentage, AudioType.Sound);
        }

        /// <summary>
        /// Resolve a clip from the AudioLibrary by a string key in "Group/Id" format (e.g. "UI/Button").
        /// Falls back to searching all groups when the key does not contain a '/'.
        /// </summary>
        public static AudioClip GetClip(string clipId)
        {
            if (audioLibrary == null) return null;

            if (TryParseSoundKey(clipId, out var key))
            {
                return audioLibrary.Get(key);
            }

            // No '/' separator – search all groups for a matching id
            foreach (var groupName in audioLibrary.GetGroupNames())
            {
                var clip = audioLibrary.Get(groupName, clipId);
                if (clip != null)
                    return clip;
            }

            return null;
        }

        /// <summary>
        /// Resolve a clip from the AudioLibrary by SoundKey.
        /// </summary>
        public static AudioClip GetClip(SoundKey key)
        {
            return audioLibrary != null ? audioLibrary.Get(key) : null;
        }

        public static bool TryPlaySound(string clipId, float volumePercentage = 1.0f, float pitch = 1.0f)
        {
            var clip = GetClip(clipId);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioController] Clip '{clipId}' was not found in the AudioLibrary catalog.");
                return false;
            }

            PlaySound(clip, volumePercentage, pitch);
            return true;
        }

        public static bool TryPlaySound(string clipId, Vector3 position, float volumePercentage = 1.0f, float pitch = 1.0f)
        {
            var clip = GetClip(clipId);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioController] Clip '{clipId}' was not found in the AudioLibrary catalog.");
                return false;
            }

            PlaySound(clip, position, volumePercentage, pitch);
            return true;
        }

        public static bool TryPlaySound(SoundKey key, float volumePercentage = 1.0f, float pitch = 1.0f)
        {
            var clip = GetClip(key);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioController] SoundKey '{key}' was not found in the AudioLibrary catalog.");
                return false;
            }

            PlaySound(clip, volumePercentage, pitch);
            return true;
        }

        public static bool TryPlaySound(SoundKey key, Vector3 position, float volumePercentage = 1.0f, float pitch = 1.0f)
        {
            var clip = GetClip(key);
            if (clip == null)
            {
                Debug.LogWarning($"[AudioController] SoundKey '{key}' was not found in the AudioLibrary catalog.");
                return false;
            }

            PlaySound(clip, position, volumePercentage, pitch);
            return true;
        }

        private static PooledSource GetAudioSource()
        {
            foreach (PooledSource source in audioSourcesPool)
            {
                if (!source.IsPlaying)
                {
                    return source;
                }
            }

            PooledSource createdSource = new PooledSource();
            audioSourcesPool.Add(createdSource);

            if (audioSourcesPool.Count > _maxPoolSize)
            {
                Debug.LogWarning($"[AudioController] Audio source pool exceeded max size ({_maxPoolSize}). " +
                                 $"Current: {audioSourcesPool.Count}. Consider increasing maxPoolSize in Init().");
            }

            return createdSource;
        }

        public static void SetVolume(AudioType audioType, float volume)
        {
            foreach (PooledSource source in audioSourcesPool)
            {
                source.OverrideVolume(audioType, volume);
            }

            volumeDictionary[audioType] = volume;

            VolumeChanged?.Invoke(audioType, volume);
        }

        public static float GetVolume(AudioType audioType)
        {
            if (volumeDictionary.ContainsKey(audioType))
                return volumeDictionary[audioType];

            return 1.0f;
        }

        public static float GetAudioVolume(AudioType audioType)
        {
            return GetVolume(audioType);
        }

        private static bool TryParseSoundKey(string clipId, out SoundKey key)
        {
            key = default;
            if (string.IsNullOrEmpty(clipId)) return false;

            int slashIndex = clipId.IndexOf('/');
            if (slashIndex <= 0 || slashIndex >= clipId.Length - 1) return false;

            key = new SoundKey(clipId.Substring(0, slashIndex), clipId.Substring(slashIndex + 1));
            return true;
        }

        private static void UnloadStatic()
        {
            audioSourcesPool = null;

            audioLibrary = null;
            audioListener = null;

            volumeDictionary = null;

            VolumeChanged = null;

            _poolRoot = null;
        }

        // ──────────────────────────────────
        // Private pooled audio source (replaces former AudioSourceCase)
        // ──────────────────────────────────
        private class PooledSource
        {
            private readonly AudioSource audioSource;
            public AudioSource AudioSource => audioSource;

            public bool IsPlaying => audioSource.isPlaying;

            private AudioType audioType;
            private float clipVolume;

            private readonly GameObject gameObject;
            public GameObject GameObject => gameObject;

            public PooledSource()
            {
                gameObject = new GameObject("[AUDIO SOURCE OBJECT]");
                if (_poolRoot != null)
                    gameObject.transform.SetParent(_poolRoot, false);

                GameObject.DontDestroyOnLoad(gameObject);

                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;

                AudioController.ApplyDefaultSettings(ref audioSource);
            }

            public void Play(AudioClip audioClip, float clipVolume, AudioType type = AudioType.Sound)
            {
                audioType = type;
                this.clipVolume = clipVolume;

                audioSource.clip = audioClip;
                audioSource.volume = clipVolume * AudioController.GetVolume(audioType);

                audioSource.Play();
            }

            public void OverrideVolume(AudioType type, float volume)
            {
                if (!audioSource.isPlaying || audioType != type) return;

                audioSource.volume = volume * clipVolume;
            }
        }

        public delegate void OnVolumeChangedCallback(AudioType audioType, float volume);
    }

    public enum AudioType
    {
        Music = 0,
        Sound = 1
    }
}
