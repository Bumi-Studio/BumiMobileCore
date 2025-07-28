using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace BumiMobile
{
    public class LoadingGraphics : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI loadingText;
        [SerializeField] Image backgroundImage;
        [SerializeField] CanvasScaler canvasScaler;
        [SerializeField] Camera loadingCamera;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            //canvasScaler.MatchSize();

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
            loadingText.DOFade(0.0f, 0.6f, unscaledTime: true);
            backgroundImage.DOFade(0.0f, 0.6f, unscaledTime: true).OnComplete(delegate
            {
                Destroy(gameObject);
            });
        }
    }
}
