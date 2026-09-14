using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BumiMobile
{
    public enum EventSpriteSwapUpdateMode
    {
        Live = 0,
        FreezeAfterStartupVisual = 1,
    }

    public enum EventCalendarVisualUpdateKind
    {
        RuntimeState = 0,
        StartupVisual = 1,
    }

    public class EventSpriteSwapController : MonoBehaviour
    {
        [SerializeField] private EventSpriteSwapUpdateMode updateMode = EventSpriteSwapUpdateMode.Live;
        public EventSpriteSwapUpdateMode UpdateMode => updateMode;
        public bool UsesStartupVisualFreeze => updateMode == EventSpriteSwapUpdateMode.FreezeAfterStartupVisual;

        [SerializeField] private EventSpriteSwapTarget[] targets = Array.Empty<EventSpriteSwapTarget>();
        public EventSpriteSwapTarget[] Targets => targets ?? Array.Empty<EventSpriteSwapTarget>();

        private readonly Dictionary<string, EventSpriteSwapTarget> targetLookup = new Dictionary<string, EventSpriteSwapTarget>(StringComparer.OrdinalIgnoreCase);
        private bool isInitialized;
        private bool isStartupVisualLocked;

        private void Awake()
        {
            InitializeTargets(true);
        }

        private void OnEnable()
        {
            InitializeTargets();
            EventCalendarService.Register(this);
        }

        private void OnDisable()
        {
            EventCalendarService.Unregister(this);
        }

        private void OnValidate()
        {
            isInitialized = false;
        }

        public void Apply(EventCalendarRuntimeState state, EventCalendarVisualUpdateKind updateKind)
        {
            InitializeTargets();

            if (updateMode == EventSpriteSwapUpdateMode.FreezeAfterStartupVisual)
            {
                if (isStartupVisualLocked)
                    return;

                if (updateKind == EventCalendarVisualUpdateKind.StartupVisual)
                    isStartupVisualLocked = true;
            }

            bool isEventActive = state != null && state.IsEventActive;
            EventSpriteSwapTarget[] currentTargets = Targets;

            for (int i = 0; i < currentTargets.Length; i++)
            {
                EventSpriteSwapTarget target = currentTargets[i];
                if (target == null)
                    continue;

                if (isEventActive && state.TryGetSprite(target.TargetId, out Sprite eventSprite) && eventSprite != null)
                {
                    target.ApplyEventSprite(eventSprite);
                    continue;
                }

                target.ApplyFallbackState();
            }
        }

        public void ResetToDefault()
        {
            InitializeTargets();

            EventSpriteSwapTarget[] currentTargets = Targets;
            for (int i = 0; i < currentTargets.Length; i++)
            {
                currentTargets[i]?.ResetToDefault();
            }
        }

        public void CaptureDefaultSprites()
        {
            EventSpriteSwapTarget[] currentTargets = Targets;
            for (int i = 0; i < currentTargets.Length; i++)
            {
                currentTargets[i]?.CaptureDefaultState();
            }

            isInitialized = false;
            InitializeTargets(true);
        }

        public void CaptureDefaultSpriteAt(int index)
        {
            EventSpriteSwapTarget[] currentTargets = Targets;
            if (index < 0 || index >= currentTargets.Length || currentTargets[index] == null)
                return;

            currentTargets[index].CaptureDefaultState();
            isInitialized = false;
            InitializeTargets(true);
        }

        private void InitializeTargets(bool force = false)
        {
            if (isInitialized && !force)
                return;

            targetLookup.Clear();

            EventSpriteSwapTarget[] currentTargets = Targets;
            for (int i = 0; i < currentTargets.Length; i++)
            {
                EventSpriteSwapTarget target = currentTargets[i];
                if (target == null)
                    continue;

                target.CaptureDefaultStateIfMissing();

                if (string.IsNullOrWhiteSpace(target.TargetId) || !target.HasAssignedTarget)
                    continue;

                if (targetLookup.ContainsKey(target.TargetId))
                {
                    Debug.LogWarning($"[Event Sprite Swap] Duplicate target id '{target.TargetId}' on {name}", this);
                    continue;
                }

                targetLookup.Add(target.TargetId, target);
            }

            isInitialized = true;
        }
    }

    [Serializable]
    public class EventSpriteSwapTarget
    {
        [SerializeField] private string targetId;
        public string TargetId => string.IsNullOrWhiteSpace(targetId) ? string.Empty : targetId.Trim();

        [SerializeField] private Image imageTarget;
        public Image ImageTarget => imageTarget;

        [SerializeField] private SpriteRenderer spriteRendererTarget;
        public SpriteRenderer SpriteRendererTarget => spriteRendererTarget;

        [SerializeField] private Sprite defaultSprite;
        public Sprite DefaultSprite => defaultSprite;

        [SerializeField] private bool hideWhenInactive;
        public bool HideWhenInactive => hideWhenInactive;

        [SerializeField] private bool useNativeSizeForEventSprite = true;
        public bool UseNativeSizeForEventSprite => useNativeSizeForEventSprite;

        [SerializeField, HideInInspector] private bool useNativeSizeForEventSpriteInitialized;

        [SerializeField, HideInInspector] private bool defaultActiveState = true;
        public bool DefaultActiveState => defaultActiveState;

        [SerializeField, HideInInspector] private bool defaultStateCaptured;
        public bool DefaultStateCaptured => defaultStateCaptured;

        [SerializeField, HideInInspector] private Vector2 defaultImageSize;
        public Vector2 DefaultImageSize => defaultImageSize;

        [SerializeField, HideInInspector] private bool defaultImageSizeCaptured;
        public bool DefaultImageSizeCaptured => defaultImageSizeCaptured;

        public bool HasAssignedTarget => imageTarget != null || spriteRendererTarget != null;
        public bool HasMultipleTargets => imageTarget != null && spriteRendererTarget != null;

        public void CaptureDefaultState()
        {
            EnsureNativeSizePreferenceInitialized();
            defaultSprite = GetCurrentSprite();
            CaptureDefaultImageSize();

            GameObject targetObject = GetTargetGameObject();
            if (targetObject != null)
            {
                defaultActiveState = targetObject.activeSelf;
                defaultStateCaptured = true;
            }
        }

        public void CaptureDefaultStateIfMissing()
        {
            EnsureNativeSizePreferenceInitialized();

            if (defaultSprite == null)
                defaultSprite = GetCurrentSprite();

            CaptureDefaultImageSizeIfMissing();

            if (defaultStateCaptured)
                return;

            GameObject targetObject = GetTargetGameObject();
            if (targetObject == null)
                return;

            defaultActiveState = targetObject.activeSelf;
            defaultStateCaptured = true;
        }

        public void ResetToDefault()
        {
            ApplySprite(defaultSprite, true);
            SetTargetActive(defaultActiveState);
        }

        public void ApplyEventSprite(Sprite sprite)
        {
            SetTargetActive(true);
            ApplySprite(sprite, false);
        }

        public void ApplyFallbackState()
        {
            ApplySprite(defaultSprite, true);

            if (hideWhenInactive)
            {
                SetTargetActive(false);
                return;
            }

            SetTargetActive(defaultActiveState);
        }

        private void ApplySprite(Sprite sprite, bool restoreDefaultImageSize)
        {
            if (imageTarget != null)
            {
                imageTarget.sprite = sprite;
                UpdateImageSize(restoreDefaultImageSize);
                return;
            }

            if (spriteRendererTarget != null)
                spriteRendererTarget.sprite = sprite;
        }

        private Sprite GetCurrentSprite()
        {
            if (imageTarget != null)
                return imageTarget.sprite;

            if (spriteRendererTarget != null)
                return spriteRendererTarget.sprite;

            return defaultSprite;
        }

        private void CaptureDefaultImageSize()
        {
            if (imageTarget == null)
                return;

            defaultImageSize = imageTarget.rectTransform.sizeDelta;
            defaultImageSizeCaptured = true;
        }

        private void CaptureDefaultImageSizeIfMissing()
        {
            if (defaultImageSizeCaptured || imageTarget == null)
                return;

            defaultImageSize = imageTarget.rectTransform.sizeDelta;
            defaultImageSizeCaptured = true;
        }

        private void UpdateImageSize(bool restoreDefaultImageSize)
        {
            if (imageTarget == null)
                return;

            RectTransform rectTransform = imageTarget.rectTransform;
            if (rectTransform == null)
                return;

            if (restoreDefaultImageSize || !useNativeSizeForEventSprite || imageTarget.sprite == null)
            {
                if (defaultImageSizeCaptured)
                    rectTransform.sizeDelta = defaultImageSize;

                return;
            }

            if (!HasFixedAnchors(rectTransform))
            {
                if (defaultImageSizeCaptured)
                    rectTransform.sizeDelta = defaultImageSize;

                return;
            }

            rectTransform.sizeDelta = GetNativeImageSize();
        }

        private void EnsureNativeSizePreferenceInitialized()
        {
            if (useNativeSizeForEventSpriteInitialized)
                return;

            useNativeSizeForEventSprite = true;
            useNativeSizeForEventSpriteInitialized = true;
        }

        private Vector2 GetNativeImageSize()
        {
            Sprite sprite = imageTarget != null ? imageTarget.sprite : null;
            if (sprite == null)
                return defaultImageSize;

            float pixelsPerUnit = imageTarget.pixelsPerUnit;
            if (pixelsPerUnit <= 0.0f)
                return defaultImageSize;

            return new Vector2(sprite.rect.width / pixelsPerUnit, sprite.rect.height / pixelsPerUnit);
        }

        private static bool HasFixedAnchors(RectTransform rectTransform)
        {
            if (rectTransform == null)
                return false;

            return Mathf.Approximately(rectTransform.anchorMin.x, rectTransform.anchorMax.x) &&
                   Mathf.Approximately(rectTransform.anchorMin.y, rectTransform.anchorMax.y);
        }

        private GameObject GetTargetGameObject()
        {
            if (imageTarget != null)
                return imageTarget.gameObject;

            if (spriteRendererTarget != null)
                return spriteRendererTarget.gameObject;

            return null;
        }

        private void SetTargetActive(bool isActive)
        {
            GameObject targetObject = GetTargetGameObject();
            if (targetObject != null && targetObject.activeSelf != isActive)
                targetObject.SetActive(isActive);
        }
    }
}
