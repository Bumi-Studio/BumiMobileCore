#pragma warning disable 0618

using System.Threading.Tasks;
using UnityEngine;

#if MODULE_LEVELPLAY
using com.unity3d.mediation;
#endif

namespace BumiMobile
{
#if MODULE_LEVELPLAY
    public class LevelPlayHandler : AdProviderHandler
    {
        private IronSourceListner eventsHolder;

        private LevelPlayBannerAd bannerAd;
        private LevelPlayInterstitialAd interstitialAd;

        public LevelPlayHandler(AdProvider moduleType) : base(moduleType) { }

        protected override async Task<bool> InitProviderAsync()
        {
            if (Monetization.VerboseLogging)
                Debug.Log("[AdsManager]: LevelPlay is trying to initialize!", adsSettings);

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            IronSource.Agent.setConsent(AdsManager.CanRequestAds());

            eventsHolder = Initializer.GameObject.AddComponent<IronSourceListner>();
            eventsHolder.Init(this);

            if (adsSettings.RewardedVideoType == AdProvider.LevelPlay)
            {
                //Add AdInfo Rewarded Video Events
                IronSourceRewardedVideoEvents.onAdOpenedEvent += RewardedVideoOnAdOpenedEvent;
                IronSourceRewardedVideoEvents.onAdClosedEvent += RewardedVideoOnAdClosedEvent;
                IronSourceRewardedVideoEvents.onAdShowFailedEvent += RewardedVideoOnAdShowFailedEvent;
                IronSourceRewardedVideoEvents.onAdRewardedEvent += RewardedVideoOnAdRewardedEvent;
            }

            LevelPlay.OnInitSuccess += (configuration) =>
            {
                if (Monetization.VerboseLogging)
                {
                    Debug.Log("[AdsManager]: LevelPlay is initialized!");

                    IronSource.Agent.validateIntegration();
                }

                OnInitCompleted();

                tcs.SetResult(true);
            };

            LevelPlay.OnInitFailed += (error) =>
            {
                Debug.LogError($"[AdsManager]: LevelPlay failed to initialized! Error: #{error.ErrorCode} {error.ErrorMessage}");

                tcs.SetResult(false);
            };

            if (Monetization.DebugMode)
                IronSource.Agent.setMetaData("is_test_suite", "enable");

            LevelPlay.Init(GetAppKey(), adFormats: new[] { LevelPlayAdFormat.REWARDED });

            return await tcs.Task;
        }

        private void OnInitCompleted()
        {
            // Banner
            LevelPlayAdSize bannerSize = LevelPlayAdSize.BANNER;
            switch (adsSettings.LevelPlayContainer.BannerType)
            {
                case LevelPlayContainer.BannerPlacementType.Large:
                    bannerSize = LevelPlayAdSize.LARGE;
                    break;
                case LevelPlayContainer.BannerPlacementType.Rectangle:
                    bannerSize = LevelPlayAdSize.MEDIUM_RECTANGLE;
                    break;
                case LevelPlayContainer.BannerPlacementType.Leaderboard:
                    bannerSize = LevelPlayAdSize.LEADERBOARD;
                    break;
            }

            LevelPlayBannerPosition bannerPosition = LevelPlayBannerPosition.BottomCenter;
            if (adsSettings.LevelPlayContainer.BannerPosition == BannerPosition.Top)
                bannerPosition = LevelPlayBannerPosition.TopCenter;

            bannerAd = new LevelPlayBannerAd(GetBannerID(), bannerSize, bannerPosition);
            bannerAd.OnAdLoaded += BannerOnAdLoadedEvent;
            bannerAd.OnAdDisplayed += BannerOnAdDisplayedEvent;

            // Ad load
            bannerAd.LoadAd();

            // Interstitial
            interstitialAd = new LevelPlayInterstitialAd(GetInterstitialID());

            // Register to events
            interstitialAd.OnAdLoaded += InterstitialOnAdLoadedEvent;
            interstitialAd.OnAdLoadFailed += InterstitialOnAdLoadFailedEvent;
            interstitialAd.OnAdDisplayed += InterstitialOnAdDisplayedEvent;
            interstitialAd.OnAdDisplayFailed += InterstitialOnAdDisplayFailedEvent;
            interstitialAd.OnAdClosed += InterstitialOnAdClosedEvent;
        }

        #region Rewarded Ad
        public override void RequestRewardedVideo()
        {
            // Do nothing
        }

        public override void ShowRewardedVideo(AdvertisementCallback callback)
        {
            IronSource.Agent.showRewardedVideo();
        }


        public override bool IsRewardedVideoLoaded()
        {
            return IronSource.Agent.isRewardedVideoAvailable();
        }

        // The Rewarded Video ad view has opened. Your activity will loose focus.
        private void RewardedVideoOnAdOpenedEvent(IronSourceAdInfo adInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                AudioListener.pause = true;

                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: RewardedVideoOnAdOpenedEvent event received");

                AdsManager.OnProviderAdDisplayed(AdProvider.LevelPlay, AdType.RewardedVideo);
            });
        }

        // The Rewarded Video ad view is about to be closed. Your activity will regain its focus.
        private void RewardedVideoOnAdClosedEvent(IronSourceAdInfo adInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                AudioListener.pause = false;

                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: RewardedVideoOnAdClosedEvent event received");

                AdsManager.OnProviderAdClosed(AdProvider.LevelPlay, AdType.RewardedVideo);
            });
        }

        // The user completed to watch the video, and should be rewarded.
        // The placement parameter will include the reward data.
        // When using server-to-server callbacks, you may ignore this event and wait for the ironSource server callback.
        private void RewardedVideoOnAdRewardedEvent(IronSourcePlacement placement, IronSourceAdInfo adInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                AdsManager.ExecuteRewardVideoCallback(true);

                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: RewardedVideoOnAdRewardedEvent event received");

                AdsManager.ResetInterstitialDelayTime();
                AdsManager.RequestRewardBasedVideo();
            });
        }

        // The rewarded video ad was failed to show.
        private void RewardedVideoOnAdShowFailedEvent(IronSourceError error, IronSourceAdInfo adInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                AdsManager.ExecuteRewardVideoCallback(false);

                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: RewardedVideoOnAdShowFailedEvent event received with message: " + error);

                HandleAdLoadFailure(AdType.RewardedVideo, error.getDescription(), ref rewardedRetryAttempt, () => RequestRewardedVideo());
            });
        }
        #endregion

        #region Interstitial
        public override void RequestInterstitial()
        {
            if (interstitialAd != null)
            {
                interstitialAd.LoadAd();
            }
        }

        public override void ShowInterstitial(AdvertisementCallback callback)
        {
            if(interstitialAd != null)
            {
                interstitialAd.ShowAd();
            }
        }

        public override bool IsInterstitialLoaded()
        {
            return interstitialAd != null && interstitialAd.IsAdReady();
        }

        private void InterstitialOnAdClosedEvent(LevelPlayAdInfo info)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                AudioListener.pause = false;

                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: InterstitialOnAdClosedEvent event received");

                AdsManager.OnProviderAdClosed(AdProvider.LevelPlay, AdType.Interstitial);

                AdsManager.ExecuteInterstitialCallback(true);

                AdsManager.ResetInterstitialDelayTime();
                AdsManager.RequestInterstitial();
            });
        }

        private void InterstitialOnAdDisplayFailedEvent(LevelPlayAdDisplayInfoError error)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: Interstitial ad failed to load an ad with error: " + error.LevelPlayError.ErrorMessage);

                HandleAdLoadFailure(AdType.Interstitial, error.LevelPlayError.ErrorMessage, ref interstitialRetryAttempt, () => RequestInterstitial());
            });
        }

        private void InterstitialOnAdDisplayedEvent(LevelPlayAdInfo info)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                AudioListener.pause = true;

                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: InterstitialOnAdDisplayedEvent event received");

                AdsManager.OnProviderAdDisplayed(AdProvider.LevelPlay, AdType.Interstitial);
            });
        }

        private void InterstitialOnAdLoadFailedEvent(LevelPlayAdError error)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: Interstitial ad failed to load an ad with error: " + error.ErrorMessage);

                HandleAdLoadFailure(AdType.Interstitial, error.ErrorMessage, ref interstitialRetryAttempt, () => RequestInterstitial());
            });
        }

        private void InterstitialOnAdLoadedEvent(LevelPlayAdInfo info)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: Interstitial ad loaded");

                interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;

                AdsManager.OnProviderAdLoaded(AdProvider.LevelPlay, AdType.Interstitial);
            });
        }

        /// <summary>
        /// Retrieves the interstitial ID based on the platform
        /// </summary>
        public string GetInterstitialID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
            return adsSettings.LevelPlayContainer.AndroidInterstitialID;
#elif UNITY_IOS
            return adsSettings.LevelPlayContainer.IOSInterstitialID;
#else
            return "unexpected_platform";
#endif
        }
        #endregion

        #region Banner
        public override void DestroyBanner()
        {
            if(bannerAd != null)
            {
                bannerAd.DestroyAd();
                bannerAd = null;
            }

            AdsManager.OnProviderAdClosed(AdProvider.LevelPlay, AdType.Banner);
        }

        public override void HideBanner()
        {
            if (bannerAd != null)
            {
                bannerAd.HideAd();
            }

            AdsManager.OnProviderAdClosed(AdProvider.LevelPlay, AdType.Banner);
        }

        public override void ShowBanner()
        {
            if(bannerAd != null)
            {
                bannerAd.ShowAd();
            }

            AdsManager.OnProviderAdDisplayed(AdProvider.LevelPlay, AdType.Banner);
        }

        private void BannerOnAdLoadedEvent(LevelPlayAdInfo info)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: BannerOnAdLoadedEvent event received");

                AdsManager.OnProviderAdLoaded(AdProvider.LevelPlay, AdType.Banner);
            });
        }

        private void BannerOnAdDisplayedEvent(LevelPlayAdInfo info)
        {
            AdsManager.OnProviderAdDisplayed(AdProvider.LevelPlay, AdType.Banner);
        }

        /// <summary>
        /// Retrieves the banner ID based on the platform
        /// </summary>
        public string GetBannerID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
            return adsSettings.LevelPlayContainer.AndroidBannerID;
#elif UNITY_IOS
            return adsSettings.LevelPlayContainer.IOSBannerID;
#else
            return "unexpected_platform";
#endif
        }
        #endregion

        public void OpenTestSuite()
        {
            IronSource.Agent.launchTestSuite();
        }

        public string GetAppKey()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
            return adsSettings.LevelPlayContainer.AndroidAppKey;
#elif UNITY_IOS
            return adsSettings.LevelPlayContainer.IOSAppKey;
#else
            return "unexpected_platform";
#endif
        }

        private class IronSourceListner : MonoBehaviour
        {
            private LevelPlayHandler ironSourceHandler;

            public void Init(LevelPlayHandler ironSourceHandler)
            {
                this.ironSourceHandler = ironSourceHandler;
            }

            private void OnApplicationPause(bool isPaused)
            {
                IronSource.Agent.onApplicationPause(isPaused);
            }
        }
    }
#endif
}