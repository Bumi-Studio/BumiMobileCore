using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BumiMobile
{
    public class LoadingGraphics : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI loadingText;
        [SerializeField] Image backgroundImage;
        [SerializeField] CanvasScaler canvasScaler;
        [SerializeField] Camera loadingCamera;

        [SerializeField, Min(0f)] float fadeDuration = 0.6f;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            OnLoading(0.0f, "Loading..");
        }

        private void OnEnable()
        {
            GameLoading.OnLoading += OnLoading;
            GameLoading.OnLoadingFinished += OnLoadingFinished;
        }

        private void OnDisable()
        {
            GameLoading.OnLoading -= OnLoading;
            GameLoading.OnLoadingFinished -= OnLoadingFinished;
        }

        private void OnLoading(float state, string message)
        {
            loadingText.text = $"{message} {state * 100.0f}%";
        }

        private void OnLoadingFinished()
        {
            StartCoroutine(FadeOutRoutine());
        }

        private IEnumerator FadeOutRoutine()
        {
            Color initialTextColor = loadingText != null ? loadingText.color : Color.white;
            Color initialBackgroundColor = backgroundImage != null ? backgroundImage.color : Color.black;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));

                if (loadingText != null)
                {
                    loadingText.color = new Color(initialTextColor.r, initialTextColor.g, initialTextColor.b, alpha * initialTextColor.a);
                }

                if (backgroundImage != null)
                {
                    backgroundImage.color = new Color(initialBackgroundColor.r, initialBackgroundColor.g, initialBackgroundColor.b, alpha * initialBackgroundColor.a);
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
