#pragma warning disable 0649

using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BumiMobile
{
    public class AdsManagerExampleScript : MonoBehaviour
    {
        private Vector2 scrollView;

        [BoxGroup("Refs")]
        [SerializeField] NotchSaveArea saveArea;

        [BoxGroup("Log")]
        [SerializeField] GameObject logPanelObject;
        [BoxGroup("Log")]
        [SerializeField] Button logOpenButton;
        [BoxGroup("Log")]
        [SerializeField] Button logCloseButton;
        [BoxGroup("Log")]
        [SerializeField] Text logText;

        [BoxGroup("Banner")]
        [SerializeField] Text bannerTitleText;
        [BoxGroup("Banner")]
        [SerializeField] Button[] bannerButtons;

        [BoxGroup("Interstitial")]
        [SerializeField] Text interstitialTitleText;
        [BoxGroup("Interstitial")]
        [SerializeField] Button[] interstitialButtons;

        [BoxGroup("RV")]
        [SerializeField] Text rewardVideoTitleText;
        [BoxGroup("RV")]
        [SerializeField] Button[] rewardVideoButtons;
        
        [BoxGroup("Components")]
        [SerializeField] Button backButton;

        private AdsSettings settings;

        private void Awake()
        {
            saveArea.Init();

            // Prepare components
            logOpenButton.onClick.AddListener(() => OnLogOpenButtonClicked());
            logCloseButton.onClick.AddListener(() => OnLogCloseButtonClicked());
            
            backButton.onClick.AddListener(() =>
            {
                SceneManager.LoadScene("Example Scene");
            });

            Application.logMessageReceived += Log;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= Log;
        }

        private void Start()
        {
            if (!Monetization.IsActive) return;
            
            Debug.Log("[AdsManager]: AdsManager is ready!");

            settings = AdsManager.Settings;

            logText.text = string.Empty;

            bannerTitleText.text = string.Format("Banner ({0})", settings.BannerType.ToString());
            if(settings.BannerType == AdProvider.Disable)
            {
                for(int i = 0; i < bannerButtons.Length; i++)
                {
                    bannerButtons[i].interactable = false; 
                }
            }

            interstitialTitleText.text = string.Format("Interstitial ({0})", settings.InterstitialType.ToString());
            if (settings.InterstitialType == AdProvider.Disable)
            {
                for (int i = 0; i < interstitialButtons.Length; i++)
                {
                    interstitialButtons[i].interactable = false;
                }
            }

            rewardVideoTitleText.text = string.Format("Rewarded Video ({0})", settings.RewardedVideoType.ToString());
            if (settings.RewardedVideoType == AdProvider.Disable)
            {
                for (int i = 0; i < rewardVideoButtons.Length; i++)
                {
                    rewardVideoButtons[i].interactable = false;
                }
            }

            GameLoading.MarkAsReadyToHide();
        }

        #region Log
        private void Log(string condition, string stackTrace, LogType type)
        {
            if(logText != null)
                logText.text = logText.text.Insert(0, condition + "\n");
        }

        private void Log(string condition)
        {
            if(logText != null)
                logText.text = logText.text.Insert(0, condition + "\n");
        }

        public void OnLogOpenButtonClicked()
        {
            logPanelObject.SetActive(true);
        }

        public void OnLogCloseButtonClicked()
        {
            logPanelObject.SetActive(false);
        }
        #endregion

        #region Buttons
        public void ShowBannerButton()
        {
            AdsManager.ShowBanner();
        }

        public void HideBannerButton()
        {
            AdsManager.HideBanner();
        }

        public void DestroyBannerButton()
        {
            AdsManager.DestroyBanner();
        }

        public void InterstitialStatusButton()
        {
            string message = "Interstitial " + (AdsManager.IsInterstitialLoaded() ? "is loaded" : "isn't loaded");

            SystemMessage.ShowMessage(message, 5.0f);

            Log("[AdsManager]: " + message);
        }

        public void RequestInterstitialButton()
        {
            AdsManager.RequestInterstitial();
        }

        public void ShowInterstitialButton()
        {
            AdsManager.ShowInterstitial( (isDisplayed) =>
            {
                Debug.Log("[AdsManager]: Interstitial " + (isDisplayed ? "is" : "isn't") + " displayed!");
            }, true);
        }

        public void RewardedVideoStatusButton()
        {
            string message = "RV " + (AdsManager.IsRewardBasedVideoLoaded() ? "is loaded" : "isn't loaded");

            SystemMessage.ShowMessage(message, 5.0f);

            Log("[AdsManager]: " + message);
        }

        public void RequestRewardedVideoButton()
        {
            AdsManager.RequestRewardBasedVideo();
        }

        public void ShowRewardedVideoButton()
        {
            AdsManager.ShowRewardBasedVideo( (hasReward) =>
            {
                if(hasReward)
                {
                    Log("[AdsManager]: Reward is received");
                }
                else
                {
                    Log("[AdsManager]: Reward isn't received");
                }
            });
        }
        #endregion

        #region UMP
        public void OnUMPResetButtonClicked()
        {
            AdsManager.ResetConsentState();
        }

        public void OnUMPStatusButtonClicked()
        {
            ConsentRequirementStatus status = AdsManager.GetConsentStatus();

            Debug.Log($"UMP Status: {status}");

            SystemMessage.ShowMessage(status.ToString(), 5.0f);
        }

        public void OnUMPRequirementButtonClicked()
        {
            bool requestState = AdsManager.CanRequestAds();

            Debug.Log($"UMP Requirement: {requestState}");

            SystemMessage.ShowMessage(requestState ? "Personalized" : "Non-personalized", 5.0f);
        }
        #endregion
    }
}