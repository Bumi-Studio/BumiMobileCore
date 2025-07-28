using UnityEngine;
using System;
using System.Threading.Tasks;

#if MODULE_ADMOB
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
#endif

namespace BumiMobile
{
#if MODULE_ADMOB
    /// <summary>
    /// Class responsible for handling AdMob ad requests and operations
    /// </summary>
    public class AdMobHandler : AdProviderHandler
    {
        // Ad objects for different types of ads
        private BannerView bannerView;
        private InterstitialAd interstitial;
        private RewardedAd rewardBasedVideo;
        private AppOpenAd appOpenAd;

        // Time when the App Open Ad expires
        private DateTime appOpenAdExpireTime = new DateTime();
        private bool appOpenAdCanShow = true;

        // Constructor setting the ad provider type
        public AdMobHandler(AdProvider providerType) : base(providerType) { }

        /// <summary>
        /// Asynchronously initializes the AdMob SDK and returns true if successful
        /// </summary>
        protected override async Task<bool> InitProviderAsync()
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            MobileAds.SetiOSAppPauseOnBackground(true);

            // Configuration for test devices, child-directed treatment, etc.
            RequestConfiguration requestConfiguration = new RequestConfiguration()
            {
                TagForChildDirectedTreatment = TagForChildDirectedTreatment.Unspecified,
                TestDeviceIds = monetizationSettings.TestDevices
            };

            MobileAds.SetRequestConfiguration(requestConfiguration);

            // Initialize the AdMob SDK
            MobileAds.Initialize(initStatus =>
            {
                if (initStatus != null)
                {
                    // Mark initialization as successful
                    tcs.SetResult(true);

                    AdsManager.CallEventInMainThread(() =>
                    {
                        // Load App Open Ad if configured
                        if (adsSettings.AdMobContainer.UseAppOpenAd)
                        {
                            LoadAppOpenAd();
                            AppStateEventNotifier.AppStateChanged += OnAppStateChanged;
                        }
                    });
                }
                else
                {
                    tcs.SetResult(false);
                }
            });

            return await tcs.Task;
        }

        /// <summary>
        /// Creates a new AdRequest (standard for all ad types)
        /// </summary>
        public AdRequest GetAdRequest()
        {
            return new AdRequest();
        }

        #region Banner
        /// <summary>
        /// Destroys the current banner ad (if any)
        /// </summary>
        public override void DestroyBanner()
        {
            bannerView?.Destroy();
        }

        /// <summary>
        /// Hides the current banner ad
        /// </summary>
        public override void HideBanner()
        {
            bannerView?.Hide();
        }

        /// <summary>
        /// Requests and displays a banner ad
        /// </summary>
        private void RequestBanner()
        {
            // Clean up any existing banner
            bannerView?.Destroy();

            AdSize adSize = GetAdSize();
            AdPosition adPosition = GetAdPosition();

            // Create a new BannerView object
            bannerView = new BannerView(GetBannerID(), adSize, adPosition);

            // Register for ad events
            RegisterBannerEvents(bannerView);

            // Load the banner ad
            bannerView.LoadAd(GetAdRequest());
        }

        /// <summary>
        /// Shows the banner ad
        /// </summary>
        public override void ShowBanner()
        {
            if (bannerView == null)
            {
                RequestBanner();
            }

            bannerView?.Show();
        }

        /// <summary>
        /// Registers events for the banner ad
        /// </summary>
        private void RegisterBannerEvents(BannerView banner)
        {
            banner.OnBannerAdLoaded += HandleAdLoaded;
            banner.OnBannerAdLoadFailed += HandleAdFailedToLoad;
            banner.OnAdPaid += HandleAdPaid;
            banner.OnAdClicked += HandleAdClicked;
            banner.OnAdFullScreenContentClosed += HandleAdClosed;
        }

        /// <summary>
        /// Handles the successful loading of a banner ad
        /// </summary>
        public void HandleAdLoaded()
        {
            AdsManager.CallEventInMainThread(() =>
            {
                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: Banner ad loaded");

                AdsManager.OnProviderAdLoaded(providerType, AdType.Banner);
            });
        }

        /// <summary>
        /// Handles a banner ad failing to load
        /// </summary>
        public void HandleAdFailedToLoad(LoadAdError error)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                // Improved error handling: Log the error message
                if (Monetization.VerboseLogging)
                    Debug.LogError($"[AdsManager]: Failed to load banner ad. Error: {error.GetMessage()}");
            });
        }

        /// <summary>
        /// Handles when a banner ad is clicked
        /// </summary>
        private void HandleAdClicked()
        {
            AdsManager.CallEventInMainThread(() =>
            {
                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: Banner ad clicked");
            });
        }

        /// <summary>
        /// Handles when the banner ad is closed
        /// </summary>
        private void HandleAdClosed()
        {
            AdsManager.CallEventInMainThread(() =>
            {
                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: Banner ad closed");

                AdsManager.OnProviderAdClosed(providerType, AdType.Banner);
            });
        }

        /// <summary>
        /// Handles when a banner ad payment is received
        /// </summary>
        private void HandleAdPaid(AdValue adValue)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                if (Monetization.VerboseLogging)
                    Debug.Log($"[AdsManager]: Banner ad paid {adValue.Value} {adValue.CurrencyCode}");
            });
        }

        /// <summary>
        /// Retrieves the ad size based on the configuration
        /// </summary>
        private AdSize GetAdSize()
        {
            return adsSettings.AdMobContainer.BannerType switch
            {
                AdMobContainer.BannerPlacementType.MediumRectangle => AdSize.MediumRectangle,
                AdMobContainer.BannerPlacementType.IABBanner => AdSize.IABBanner,
                AdMobContainer.BannerPlacementType.Leaderboard => AdSize.Leaderboard,
                _ => AdSize.Banner,
            };
        }

        /// <summary>
        /// Retrieves the ad position based on the configuration
        /// </summary>
        private AdPosition GetAdPosition()
        {
            return adsSettings.AdMobContainer.BannerPosition switch
            {
                BannerPosition.Top => AdPosition.Top,
                _ => AdPosition.Bottom,
            };
        }

        /// <summary>
        /// Retrieves the banner ID based on the platform
        /// </summary>
        public string GetBannerID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
            return adsSettings.AdMobContainer.AndroidBannerID;
#elif UNITY_IOS
            return adsSettings.AdMobContainer.IOSBannerID;
#else
            return "unexpected_platform";
#endif
        }
        #endregion

        #region Interstitial
        /// <summary>
        /// Requests and loads an interstitial ad
        /// </summary>
        public override void RequestInterstitial()
        {
            // Clean up any existing interstitial
            interstitial?.Destroy();

            // Load a new interstitial ad
            InterstitialAd.Load(GetInterstitialID(), GetAdRequest(), (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    HandleAdLoadFailure(AdType.Interstitial, error.GetMessage(), ref interstitialRetryAttempt, RequestInterstitial);

                    return;
                }

                interstitial = ad;
                interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;

                AdsManager.OnProviderAdLoaded(providerType, AdType.Interstitial);

                // Register interstitial events
                RegisterInterstitialEvents(interstitial);
            });
        }

        /// <summary>
        /// Displays the interstitial ad
        /// </summary>
        public override void ShowInterstitial(AdvertisementCallback callback)
        {
            interstitial?.Show();
        }

        /// <summary>
        /// Registers events for the interstitial ad
        /// </summary>
        private void RegisterInterstitialEvents(InterstitialAd ad)
        {
            ad.OnAdFullScreenContentOpened += HandleInterstitialOpened;
            ad.OnAdFullScreenContentClosed += HandleInterstitialClosed;
            ad.OnAdClicked += HandleInterstitialClicked;
        }

        /// <summary>
        /// Handles when the interstitial ad is successfully opened
        /// </summary>
        private void HandleInterstitialOpened()
        {
            AdsManager.CallEventInMainThread(() =>
            {
                appOpenAdCanShow = false;

                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: Interstitial ad opened");

                AdsManager.OnProviderAdDisplayed(providerType, AdType.Interstitial);
            });
        }

        /// <summary>
        /// Handles when the interstitial ad is closed
        /// </summary>
        private void HandleInterstitialClosed()
        {
            AdsManager.CallEventInMainThread(() =>
            {
                appOpenAdCanShow = false;

                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: Interstitial ad closed");

                AdsManager.OnProviderAdClosed(providerType, AdType.Interstitial);
                AdsManager.ExecuteInterstitialCallback(true);

                // Reset the interstitial delay and request a new ad
                AdsManager.ResetInterstitialDelayTime();

                RequestInterstitial();
            });
        }

        /// <summary>
        /// Handles when the interstitial ad is clicked
        /// </summary>
        private void HandleInterstitialClicked()
        {
            AdsManager.CallEventInMainThread(() =>
            {
                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: Interstitial ad clicked");
            });
        }

        /// <summary>
        /// Checks if an interstitial ad is loaded and ready to be shown
        /// </summary>
        public override bool IsInterstitialLoaded()
        {
            return interstitial != null && interstitial.CanShowAd();
        }

        /// <summary>
        /// Retrieves the interstitial ID based on the platform
        /// </summary>
        public string GetInterstitialID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
            return adsSettings.AdMobContainer.AndroidInterstitialID;
#elif UNITY_IOS
            return adsSettings.AdMobContainer.IOSInterstitialID;
#else
            return "unexpected_platform";
#endif
        }
        #endregion

        #region Rewarded Video
        /// <summary>
        /// Requests and loads a rewarded video ad
        /// </summary>
        public override void RequestRewardedVideo()
        {
            RewardedAd.Load(GetRewardedVideoID(), GetAdRequest(), (RewardedAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    HandleAdLoadFailure(AdType.RewardedVideo, error.GetMessage(), ref rewardedRetryAttempt, RequestRewardedVideo);

                    return;
                }

                rewardBasedVideo = ad;
                rewardedRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;

                AdsManager.OnProviderAdLoaded(providerType, AdType.RewardedVideo);

                // Register rewarded video events
                RegisterRewardedVideoEvents(rewardBasedVideo);
            });
        }

        /// <summary>
        /// Shows the rewarded video ad
        /// </summary>
        public override void ShowRewardedVideo(AdvertisementCallback callback)
        {
            rewardBasedVideo?.Show(reward =>
            {
                AdsManager.CallEventInMainThread(() =>
                {
                    AdsManager.OnProviderAdDisplayed(providerType, AdType.RewardedVideo);
                    AdsManager.ExecuteRewardVideoCallback(true);

                    if (Monetization.VerboseLogging)
                        Debug.Log("[AdsManager]: Rewarded video completed");

                    // Reset the delay and request a new rewarded video
                    AdsManager.ResetInterstitialDelayTime();
                    RequestRewardedVideo();
                });
            });
        }

        /// <summary>
        /// Registers events for the rewarded video ad
        /// </summary>
        private void RegisterRewardedVideoEvents(RewardedAd ad)
        {
            ad.OnAdFullScreenContentFailed += HandleRewardBasedVideoFailedToShow;
            ad.OnAdFullScreenContentOpened += HandleRewardBasedVideoOpened;
            ad.OnAdFullScreenContentClosed += HandleRewardBasedVideoClosed;
            ad.OnAdClicked += HandleRewardBasedVideoClicked;
        }

        /// <summary>
        /// Handles when the rewarded video fails to show
        /// </summary>
        private void HandleRewardBasedVideoFailedToShow(AdError error)
        {
            AdsManager.CallEventInMainThread(() =>
            {
                AdsManager.ExecuteRewardVideoCallback(false);

                HandleAdLoadFailure(AdType.RewardedVideo, error.GetMessage(), ref rewardedRetryAttempt, RequestRewardedVideo);
            });
        }

        /// <summary>
        /// Handles when the rewarded video is opened
        /// </summary>
        private void HandleRewardBasedVideoOpened()
        {
            AdsManager.CallEventInMainThread(() =>
            {
                appOpenAdCanShow = false;

                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: Rewarded video opened");
            });
        }

        /// <summary>
        /// Handles when the rewarded video is closed
        /// </summary>
        private void HandleRewardBasedVideoClosed()
        {
            AdsManager.CallEventInMainThread(() =>
            {
                appOpenAdCanShow = false;

                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: Rewarded video closed");

                AdsManager.OnProviderAdClosed(providerType, AdType.RewardedVideo);
            });
        }

        /// <summary>
        /// Handles when the rewarded video ad is clicked
        /// </summary>
        private void HandleRewardBasedVideoClicked()
        {
            AdsManager.CallEventInMainThread(() =>
            {
                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: Rewarded video clicked");
            });
        }

        /// <summary>
        /// Checks if a rewarded video is loaded and ready to be shown
        /// </summary>
        public override bool IsRewardedVideoLoaded()
        {
            return rewardBasedVideo != null && rewardBasedVideo.CanShowAd();
        }

        /// <summary>
        /// Retrieves the rewarded video ID based on the platform
        /// </summary>
        public string GetRewardedVideoID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
            return adsSettings.AdMobContainer.AndroidRewardedVideoID;
#elif UNITY_IOS
            return adsSettings.AdMobContainer.IOSRewardedVideoID;
#else
            return "unexpected_platform";
#endif
        }
        #endregion

        #region App Open Ad
        /// <summary>
        /// Checks if the app open ad is available and hasn't expired
        /// </summary>
        private bool IsAppOpenAdAvailable()
        {
            return appOpenAd != null && appOpenAd.CanShowAd() && DateTime.Now < appOpenAdExpireTime;
        }

        /// <summary>
        /// Handles app state changes and shows app open ad if available
        /// </summary>
        private void OnAppStateChanged(AppState state)
        {
            if (Monetization.VerboseLogging)
                Debug.Log($"[AdsManager]: App State changed to : {state}");

            Debug.Log($"[AdsManager]: State: {state}; CanShow: {appOpenAdCanShow}");

            if(state == AppState.Foreground)
            {
                if (appOpenAdCanShow && IsAppOpenAdAvailable())
                {
                    appOpenAd.Show();

                    AdsManager.DisableBanner();
                }

                appOpenAdCanShow = true;
            }
            else
            {
                appOpenAdCanShow = true;
            }
        }

        /// <summary>
        /// Loads an app open ad
        /// </summary>
        private void LoadAppOpenAd()
        {
            // Clean up any existing app open ad
            appOpenAd?.Destroy();
            appOpenAd = null;

            Debug.Log("[AdsManager]: Loading the app open ad.");

            AppOpenAd.Load(GetAppOpenID(), GetAdRequest(), (AppOpenAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError($"[AdsManager]: App open ad failed to load with error: {error}");
                    return;
                }

                appOpenAd = ad;
                appOpenAdExpireTime = DateTime.Now + TimeSpan.FromHours(adsSettings.AdMobContainer.AppOpenAdExpirationHoursTime);

                // Register app open ad events
                RegisterAppOpenAdEvents(ad);
            });
        }

        /// <summary>
        /// Registers events for the app open ad
        /// </summary>
        private void RegisterAppOpenAdEvents(AppOpenAd ad)
        {
            ad.OnAdPaid += HandleAppOpenPaid;
            ad.OnAdImpressionRecorded += HandleAppOpenImpressionRecorded;
            ad.OnAdClicked += HandleAppOpenAdClicked;
            ad.OnAdFullScreenContentOpened += HandleAppOpenAdContentOpened;
            ad.OnAdFullScreenContentClosed += HandleAppOpenAdContentClosed;
            ad.OnAdFullScreenContentFailed += HandleAppOpenAdConentFailed;
        }

        /// <summary>
        /// Handles when the app open ad fails to open
        /// </summary>
        private void HandleAppOpenAdConentFailed(AdError error)
        {
            if (Monetization.VerboseLogging)
                Debug.LogError($"[AdsManager]: App open ad failed to open with error: {error}");

            // Reload the ad after failure
            LoadAppOpenAd();

            AdsManager.EnableBanner();
        }

        /// <summary>
        /// Handles when the app open ad is closed
        /// </summary>
        private void HandleAppOpenAdContentClosed()
        {
            if (Monetization.VerboseLogging)
                Debug.Log("[AdsManager]: App open ad closed.");

            // Reload the ad after closing
            LoadAppOpenAd();

            AdsManager.EnableBanner();
        }

        /// <summary>
        /// Handles when the app open ad is opened
        /// </summary>
        private void HandleAppOpenAdContentOpened()
        {
            if (Monetization.VerboseLogging)
                Debug.Log("[AdsManager]: App open ad opened.");
        }

        /// <summary>
        /// Handles when the app open ad is clicked
        /// </summary>
        private void HandleAppOpenAdClicked()
        {
            if (Monetization.VerboseLogging)
                Debug.Log("[AdsManager]: App open ad clicked.");
        }

        /// <summary>
        /// Handles when the app open ad records an impression
        /// </summary>
        private void HandleAppOpenImpressionRecorded()
        {
            if (Monetization.VerboseLogging)
                Debug.Log("[AdsManager]: App open ad recorded an impression.");
        }

        /// <summary>
        /// Handles when the app open ad records a payment
        /// </summary>
        /// <param name="value"></param>
        private void HandleAppOpenPaid(AdValue value)
        {
            if (Monetization.VerboseLogging)
                Debug.Log($"[AdsManager]: App open ad paid {value.Value} {value.CurrencyCode}.");
        }

        /// <summary>
        /// Retrieves the app open ad ID based on the platform
        /// </summary>
        public string GetAppOpenID()
        {
#if UNITY_EDITOR
            return "unused";
#elif UNITY_ANDROID
            return adsSettings.AdMobContainer.AndroidAppOpenAdID;
#elif UNITY_IOS
            return adsSettings.AdMobContainer.IOSAppOpenAdID;
#else
            return "unexpected_platform";
#endif
        }
        #endregion
    }
#endif
}
