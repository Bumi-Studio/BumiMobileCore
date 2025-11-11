using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BumiMobile
{
    [RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
    public class SystemMessage : MonoBehaviour
    {
        private static SystemMessage floatingMessage;

        [Header("Messages")]
        [SerializeField] RectTransform messagePanelRectTransform;
        [SerializeField] TextMeshProUGUI messageText;

        [Header("Loading")]
        [SerializeField] GameObject loadingPanelObject;
        [SerializeField] TextMeshProUGUI loadingStatusText;
        [SerializeField] RectTransform loadingIconRectTransform;

    private Coroutine animationCoroutine;

        private CanvasGroup messagePanelCanvasGroup;

        private bool isLoadingActive;

        private void Start()
        {
            if (floatingMessage != null) return;

            floatingMessage = this;

            CanvasScaler canvasScaler = gameObject.GetComponent<CanvasScaler>();
            if (canvasScaler != null)
            {
                canvasScaler.matchWidthOrHeight = ((float)Screen.width / Screen.height) > (9f / 16f) ? 1.0f : 0.0f;
            }

            messagePanelCanvasGroup = gameObject.AddComponent<CanvasGroup>();

            RegisterClickHandler(messageText);

            loadingPanelObject.SetActive(false);
            messagePanelRectTransform.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (isLoadingActive)
            {
                loadingIconRectTransform.Rotate(0, 0, -50 * Time.deltaTime);
            }
        }

        private void OnPanelClick()
        {
            if (floatingMessage == null)
                return;

            floatingMessage.StopAnimation();
            floatingMessage.animationCoroutine = floatingMessage.StartCoroutine(floatingMessage.FadeOutAndDisable(0.3f));
        }

        public static void ShowMessage(string message, float duration = 2.5f)
        {
            if(floatingMessage != null)
            {
                if (floatingMessage.isLoadingActive) return;

                floatingMessage.StopAnimation();

                floatingMessage.messageText.text = message;

                floatingMessage.messagePanelRectTransform.gameObject.SetActive(true);

                floatingMessage.messagePanelCanvasGroup.alpha = 1.0f;
                floatingMessage.animationCoroutine = floatingMessage.StartCoroutine(floatingMessage.HideMessageRoutine(duration));
            }
            else
            {
                Debug.Log("[System Message]: " + message);
                Debug.LogError("[System Message]: ShowMessage() method has called, but module isn't initialized!");
            }
        }

        public static void ShowLoadingPanel()
        {
            if (floatingMessage == null) return;
            if (floatingMessage.isLoadingActive) return;

            // Disable message panel if it is active
            floatingMessage.StopAnimation();
            floatingMessage.messagePanelRectTransform.gameObject.SetActive(false);
            floatingMessage.messagePanelCanvasGroup.alpha = 0f;

            // Activate loading
            floatingMessage.isLoadingActive = true;
            floatingMessage.loadingPanelObject.SetActive(true);
        }

        public static void ChangeLoadingMessage(string message)
        {
            if (floatingMessage == null) return;

            floatingMessage.loadingStatusText.text = message;
        }

        public static void HideLoadingPanel()
        {
            if (floatingMessage == null) return;

            // Disable loading
            floatingMessage.isLoadingActive = false;
            floatingMessage.loadingPanelObject.SetActive(false);
        }

        private IEnumerator HideMessageRoutine(float visibleDuration)
        {
            yield return new WaitForSecondsRealtime(visibleDuration);

            yield return FadeCanvasGroup(messagePanelCanvasGroup, 0f, 0.5f);

            messagePanelRectTransform.gameObject.SetActive(false);
            animationCoroutine = null;
        }

        private IEnumerator FadeOutAndDisable(float duration)
        {
            yield return FadeCanvasGroup(messagePanelCanvasGroup, 0f, duration);
            messagePanelRectTransform.gameObject.SetActive(false);
            animationCoroutine = null;
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float targetAlpha, float duration)
        {
            if (canvasGroup == null)
            {
                yield break;
            }

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            if (duration <= 0f)
            {
                canvasGroup.alpha = targetAlpha;
                yield break;
            }

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = EaseOutCirc(t);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, easedT);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        private void StopAnimation()
        {
            if (animationCoroutine == null)
                return;

            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        private void RegisterClickHandler(Component component)
        {
            if (component == null)
                return;

            EventTrigger eventTrigger = component.GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = component.gameObject.AddComponent<EventTrigger>();
            }

            eventTrigger.triggers ??= new List<EventTrigger.Entry>();
            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerClick
            };

            entry.callback.AddListener(_ => OnPanelClick());
            eventTrigger.triggers.Add(entry);
        }

        private static float EaseOutCirc(float t)
        {
            float clamped = Mathf.Clamp01(t);
            float inverted = clamped - 1f;
            return Mathf.Sqrt(1f - (inverted * inverted));
        }
    }
}
