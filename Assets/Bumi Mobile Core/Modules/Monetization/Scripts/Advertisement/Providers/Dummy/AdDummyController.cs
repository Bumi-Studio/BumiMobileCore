#pragma warning disable 0649

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace BumiMobile
{
    public class AdDummyController : MonoBehaviour
    {
        private GameObject bannerObject;

        private GameObject interstitialObject;
        private Button interstitialCloseButton;

        private GameObject rewardedVideoObject;
        private Button rewardedVideoCloseButton;
        private Button rewardedVideoRewardButton;

        private RectTransform bannerRectTransform;

        public void Init(AdsSettings settings)
        {
            bannerRectTransform = (RectTransform)bannerObject.transform;

            interstitialCloseButton.AddEvent(EventTriggerType.PointerDown, (data) => CloseInterstitialButton());
            rewardedVideoCloseButton.AddEvent(EventTriggerType.PointerDown, (data) => CloseRewardedVideoButton());
            rewardedVideoRewardButton.AddEvent(EventTriggerType.PointerDown, (data) => GetRewardButton());

            // Toggle editor visibility
            gameObject.ToggleVisibility(true);
            gameObject.TogglePicking(true);

            DontDestroyOnLoad(gameObject);

            switch (settings.DummyContainer.BannerPosition)
            {
                case BannerPosition.Bottom:
                    bannerRectTransform.pivot = new Vector2(0.5f, 0.0f);

                    bannerRectTransform.anchorMin = new Vector2(0.0f, 0.0f);
                    bannerRectTransform.anchorMax = new Vector2(1.0f, 0.0f);

                    bannerRectTransform.anchoredPosition = Vector2.zero;
                    break;
                case BannerPosition.Top:
                    bannerRectTransform.pivot = new Vector2(0.5f, 1.0f);

                    bannerRectTransform.anchorMin = new Vector2(0.0f, 1.0f);
                    bannerRectTransform.anchorMax = new Vector2(1.0f, 1.0f);

                    bannerRectTransform.anchoredPosition = Vector2.zero;
                    break;
            }
        }

        public void ShowBanner()
        {
            bannerObject.SetActive(true);
        }

        public void HideBanner()
        {
            bannerObject.SetActive(false);
        }

        public void ShowInterstitial()
        {
            interstitialObject.SetActive(true);
        }

        public void CloseInterstitial()
        {
            interstitialObject.SetActive(false);

            AdsManager.OnProviderAdClosed(AdProvider.Dummy, AdType.Interstitial);
        }

        public void ShowRewardedVideo()
        {
            rewardedVideoObject.SetActive(true);
        }

        public void CloseRewardedVideo()
        {
            rewardedVideoObject.SetActive(false);

            AdsManager.OnProviderAdClosed(AdProvider.Dummy, AdType.RewardedVideo);
        }

        #region Buttons
        public void CloseInterstitialButton()
        {
            AdsManager.ExecuteInterstitialCallback(true);

            CloseInterstitial();
        }

        public void CloseRewardedVideoButton()
        {
            AdsManager.ExecuteRewardVideoCallback(false);

            CloseRewardedVideo();
        }

        public void GetRewardButton()
        {
            AdsManager.ExecuteRewardVideoCallback(true);

            CloseRewardedVideo();
        }
        #endregion

        public static AdDummyController CreateObject()
        {
            Color backgroundColor = new Color(0.211f, 0.211f, 0.321f, 1.0f);
            Color mainColor = new Color(0.207f, 0.305f, 0.717f, 1.0f);

#if UNITY_EDITOR
            backgroundColor = CoreEditor.AdsDummyBackgroundColor;
            mainColor = CoreEditor.AdsDummyMainColor;
#endif

            GameObject go = new GameObject("[ADS DUMMY CANVAS]");

            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;

            CanvasScaler canvasScaler = go.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1080, 1920);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.MatchSize();

            GraphicRaycaster graphicRaycaster = go.AddComponent<GraphicRaycaster>();
            graphicRaycaster.ignoreReversedGraphics = true;
            graphicRaycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

            // Banner
            GameObject bannerObject = new GameObject("Banner");
            bannerObject.SetActive(false);

            RectTransform bannerRectTransform = bannerObject.AddComponent<RectTransform>();
            bannerRectTransform.SetParent(go.transform);
            bannerRectTransform.anchorMin = new Vector2(0, 1);
            bannerRectTransform.anchorMax = new Vector2(1, 1);
            bannerRectTransform.pivot = new Vector2(0.5f, 1);
            bannerRectTransform.sizeDelta = new Vector2(0, 84);
            bannerRectTransform.anchoredPosition = new Vector2(0, 0);

            Image bannerImage = bannerObject.AddComponent<Image>();
            bannerImage.color = mainColor;

            // Banner Text
            GameObject bannerTextObject = new GameObject("Text");

            RectTransform bannerTextRectTransform = bannerTextObject.AddComponent<RectTransform>();
            bannerTextRectTransform.SetParent(bannerRectTransform);
            bannerTextRectTransform.anchorMin = new Vector2(0, 0);
            bannerTextRectTransform.anchorMax = new Vector2(1, 1);
            bannerTextRectTransform.pivot = new Vector2(0.5f, 0.5f);
            bannerTextRectTransform.sizeDelta = new Vector2(0, 0);
            bannerTextRectTransform.anchoredPosition = new Vector2(0, 0);

            TextMeshProUGUI bannerText = bannerTextObject.AddComponent<TextMeshProUGUI>();
            bannerText.text = "TEST BANNER";
            bannerText.alignment = TextAlignmentOptions.Converted;
            bannerText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            bannerText.verticalAlignment = VerticalAlignmentOptions.Geometry;
            bannerText.fontSize = 30;
            bannerText.fontWeight = FontWeight.Bold;

            // Interstitial
            GameObject interstitialObject = new GameObject("Interstitial");
            interstitialObject.SetActive(false);

            RectTransform interstitialRectTransform = interstitialObject.AddComponent<RectTransform>();
            interstitialRectTransform.SetParent(go.transform);
            interstitialRectTransform.anchorMin = new Vector2(0, 0);
            interstitialRectTransform.anchorMax = new Vector2(1, 1);
            interstitialRectTransform.pivot = new Vector2(0.5f, 0.5f);
            interstitialRectTransform.sizeDelta = new Vector2(0, 0);
            interstitialRectTransform.anchoredPosition = new Vector2(0, 0);

            Image interstitialImage = interstitialObject.AddComponent<Image>();
            interstitialImage.color = backgroundColor;

            // Interstitial Title
            GameObject interstitialTitleObject = new GameObject("Title");

            RectTransform interstitialTitleRectTransform = interstitialTitleObject.AddComponent<RectTransform>();
            interstitialTitleRectTransform.SetParent(interstitialRectTransform);
            interstitialTitleRectTransform.anchorMin = new Vector2(0, 0.5f);
            interstitialTitleRectTransform.anchorMax = new Vector2(1, 0.5f);
            interstitialTitleRectTransform.pivot = new Vector2(0.5f, 0.5f);
            interstitialTitleRectTransform.sizeDelta = new Vector2(0, 100);
            interstitialTitleRectTransform.anchoredPosition = new Vector2(0, 200);

            TextMeshProUGUI interstitialText = interstitialTitleObject.AddComponent<TextMeshProUGUI>();
            interstitialText.text = "TEST INTERSTITIAL";
            interstitialText.alignment = TextAlignmentOptions.Converted;
            interstitialText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            interstitialText.verticalAlignment = VerticalAlignmentOptions.Geometry;
            interstitialText.fontSize = 62;
            interstitialText.fontWeight = FontWeight.Bold;

            // Interstitial Close Button
            GameObject interstitialCloseButtonObject = new GameObject("Close Button");

            RectTransform interstitialCloseButtonRectTransform = interstitialCloseButtonObject.AddComponent<RectTransform>();
            interstitialCloseButtonRectTransform.SetParent(interstitialRectTransform);
            interstitialCloseButtonRectTransform.anchorMin = new Vector2(0.5f, 0);
            interstitialCloseButtonRectTransform.anchorMax = new Vector2(0.5f, 0);
            interstitialCloseButtonRectTransform.pivot = new Vector2(0.5f, 0);
            interstitialCloseButtonRectTransform.sizeDelta = new Vector2(600, 200);
            interstitialCloseButtonRectTransform.anchoredPosition = new Vector2(0, 360);

            Image interstitialCloseButtonImage = interstitialCloseButtonObject.AddComponent<Image>();
            interstitialCloseButtonImage.color = mainColor;

            Button interstitialCloseButton = interstitialCloseButtonObject.AddComponent<Button>();
            interstitialCloseButton.targetGraphic = interstitialCloseButtonImage;

            // Interstitial Close Button Text
            GameObject interstitialCloseButtonTextObject = new GameObject("Text");

            RectTransform interstitialCloseButtonTextRectTransform = interstitialCloseButtonTextObject.AddComponent<RectTransform>();
            interstitialCloseButtonTextRectTransform.SetParent(interstitialCloseButtonRectTransform);
            interstitialCloseButtonTextRectTransform.anchorMin = new Vector2(0, 0);
            interstitialCloseButtonTextRectTransform.anchorMax = new Vector2(1, 1);
            interstitialCloseButtonTextRectTransform.pivot = new Vector2(0.5f, 0.5f);
            interstitialCloseButtonTextRectTransform.sizeDelta = new Vector2(0, 0);
            interstitialCloseButtonTextRectTransform.anchoredPosition = new Vector2(0, 0);

            TextMeshProUGUI interstitialCloseButtonText = interstitialCloseButtonTextObject.AddComponent<TextMeshProUGUI>();
            interstitialCloseButtonText.text = "CLOSE";
            interstitialCloseButtonText.alignment = TextAlignmentOptions.Converted;
            interstitialCloseButtonText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            interstitialCloseButtonText.verticalAlignment = VerticalAlignmentOptions.Geometry;
            interstitialCloseButtonText.fontSize = 60;
            interstitialCloseButtonText.fontWeight = FontWeight.Bold;

            // Rewarded Video
            GameObject rvObject = new GameObject("Rewarded Video");
            rvObject.SetActive(false);

            RectTransform rvRectTransform = rvObject.AddComponent<RectTransform>();
            rvRectTransform.SetParent(go.transform);
            rvRectTransform.anchorMin = new Vector2(0, 0);
            rvRectTransform.anchorMax = new Vector2(1, 1);
            rvRectTransform.pivot = new Vector2(0.5f, 0.5f);
            rvRectTransform.sizeDelta = new Vector2(0, 0);
            rvRectTransform.anchoredPosition = new Vector2(0, 0);

            Image rvImage = rvObject.AddComponent<Image>();
            rvImage.color = backgroundColor;

            // RV Title
            GameObject rvTitleObject = new GameObject("Title");

            RectTransform rvTitleRectTransform = rvTitleObject.AddComponent<RectTransform>();
            rvTitleRectTransform.SetParent(rvRectTransform);
            rvTitleRectTransform.anchorMin = new Vector2(0, 0.5f);
            rvTitleRectTransform.anchorMax = new Vector2(1, 0.5f);
            rvTitleRectTransform.pivot = new Vector2(0.5f, 0.5f);
            rvTitleRectTransform.sizeDelta = new Vector2(0, 100);
            rvTitleRectTransform.anchoredPosition = new Vector2(0, 200);

            TextMeshProUGUI rvText = rvTitleObject.AddComponent<TextMeshProUGUI>();
            rvText.text = "TEST REWARDED VIDEO";
            rvText.alignment = TextAlignmentOptions.Converted;
            rvText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            rvText.verticalAlignment = VerticalAlignmentOptions.Geometry;
            rvText.fontSize = 62;
            rvText.fontWeight = FontWeight.Bold;

            // RV Close Button
            GameObject rvCloseButtonObject = new GameObject("Close Button");

            RectTransform rvCloseButtonRectTransform = rvCloseButtonObject.AddComponent<RectTransform>();
            rvCloseButtonRectTransform.SetParent(rvRectTransform);
            rvCloseButtonRectTransform.anchorMin = new Vector2(0.5f, 0);
            rvCloseButtonRectTransform.anchorMax = new Vector2(0.5f, 0);
            rvCloseButtonRectTransform.pivot = new Vector2(0.5f, 0);
            rvCloseButtonRectTransform.sizeDelta = new Vector2(600, 200);
            rvCloseButtonRectTransform.anchoredPosition = new Vector2(0, 360);

            Image rvCloseButtonImage = rvCloseButtonObject.AddComponent<Image>();
            rvCloseButtonImage.color = mainColor;

            Button rvCloseButton = rvCloseButtonObject.AddComponent<Button>();
            rvCloseButton.targetGraphic = rvCloseButtonImage;

            // RV Close Button Text
            GameObject rvCloseButtonTextObject = new GameObject("Text");

            RectTransform rvCloseButtonTextRectTransform = rvCloseButtonTextObject.AddComponent<RectTransform>();
            rvCloseButtonTextRectTransform.SetParent(rvCloseButtonRectTransform);
            rvCloseButtonTextRectTransform.anchorMin = new Vector2(0, 0);
            rvCloseButtonTextRectTransform.anchorMax = new Vector2(1, 1);
            rvCloseButtonTextRectTransform.pivot = new Vector2(0.5f, 0.5f);
            rvCloseButtonTextRectTransform.sizeDelta = new Vector2(0, 0);
            rvCloseButtonTextRectTransform.anchoredPosition = new Vector2(0, 0);

            TextMeshProUGUI rvCloseButtonText = rvCloseButtonTextObject.AddComponent<TextMeshProUGUI>();
            rvCloseButtonText.text = "CLOSE";
            rvCloseButtonText.alignment = TextAlignmentOptions.Converted;
            rvCloseButtonText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            rvCloseButtonText.verticalAlignment = VerticalAlignmentOptions.Geometry;
            rvCloseButtonText.fontSize = 60;
            rvCloseButtonText.fontWeight = FontWeight.Bold;

            // RV Reward Button
            GameObject rvRewardButtonObject = new GameObject("Get Reward Button");

            RectTransform rvRewardButtonRectTransform = rvRewardButtonObject.AddComponent<RectTransform>();
            rvRewardButtonRectTransform.SetParent(rvRectTransform);
            rvRewardButtonRectTransform.anchorMin = new Vector2(0.5f, 0);
            rvRewardButtonRectTransform.anchorMax = new Vector2(0.5f, 0);
            rvRewardButtonRectTransform.pivot = new Vector2(0.5f, 0);
            rvRewardButtonRectTransform.sizeDelta = new Vector2(600, 200);
            rvRewardButtonRectTransform.anchoredPosition = new Vector2(0, 600);

            Image rvRewardButtonImage = rvRewardButtonObject.AddComponent<Image>();
            rvRewardButtonImage.color = mainColor;

            Button rvRewardButton = rvRewardButtonObject.AddComponent<Button>();
            rvRewardButton.targetGraphic = rvRewardButtonImage;

            // RV Reward Button Text
            GameObject rvRewardButtonTextObject = new GameObject("Text");

            RectTransform rvRewardButtonTextRectTransform = rvRewardButtonTextObject.AddComponent<RectTransform>();
            rvRewardButtonTextRectTransform.SetParent(rvRewardButtonRectTransform);
            rvRewardButtonTextRectTransform.anchorMin = new Vector2(0, 0);
            rvRewardButtonTextRectTransform.anchorMax = new Vector2(1, 1);
            rvRewardButtonTextRectTransform.pivot = new Vector2(0.5f, 0.5f);
            rvRewardButtonTextRectTransform.sizeDelta = new Vector2(0, 0);
            rvRewardButtonTextRectTransform.anchoredPosition = new Vector2(0, 0);

            TextMeshProUGUI rvRewardButtonText = rvRewardButtonTextObject.AddComponent<TextMeshProUGUI>();
            rvRewardButtonText.text = "GET REWARD";
            rvRewardButtonText.alignment = TextAlignmentOptions.Converted;
            rvRewardButtonText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            rvRewardButtonText.verticalAlignment = VerticalAlignmentOptions.Geometry;
            rvRewardButtonText.fontSize = 60;
            rvRewardButtonText.fontWeight = FontWeight.Bold;

            AdDummyController controller = go.AddComponent<AdDummyController>();
            controller.bannerObject = bannerObject;

            controller.interstitialObject = interstitialObject;
            controller.interstitialCloseButton = interstitialCloseButton;

            controller.rewardedVideoObject = rvObject;
            controller.rewardedVideoCloseButton = rvCloseButton;
            controller.rewardedVideoRewardButton = rvRewardButton;

            return controller;
        }
    }
}