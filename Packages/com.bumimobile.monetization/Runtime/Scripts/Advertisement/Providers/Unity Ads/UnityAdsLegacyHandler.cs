#pragma warning disable 0414

using System.Threading.Tasks;
using UnityEngine;

#if MODULE_UNITYADS
using UnityEngine.Advertisements;
#endif

namespace BumiMobile
{
#if MODULE_UNITYADS
    /// <summary>
    /// Handles Unity Ads integration for legacy Unity Ads implementation. 
    /// Manages initialization, ad loading, and display logic for banner, interstitial, and rewarded video ads.
    /// </summary>
    public class UnityAdsLegacyHandler : AdProviderHandler
    {
        private const int INIT_CHECK_MAX_ATTEMPT_AMOUNT = 5;

        private static string placementBannerID;
        private static string placementInterstitialID;
        private static string placementRewardedVideoID;
        private static string appId;

        private bool interstitialIsLoaded;
        private bool rewardVideoIsLoaded;

        private int initializationAttemptCount = 0;

        private UnityAdvertismentListener unityAdvertisment;

        /// <summary>
        /// Constructor that initializes the handler with the specified ad provider type.
        /// </summary>
        public UnityAdsLegacyHandler(AdProvider moduleType) : base(moduleType) { }

        /// <summary>
        /// Asynchronously initializes Unity Ads. 
        /// Ensures GDPR compliance by setting meta-data for user consent. 
        /// </summary>
        protected override async Task<bool> InitProviderAsync()
        {
            if (Monetization.VerboseLogging)
                Debug.Log("[AdsManager]: Unity Ads is trying to initialize!", adsSettings);

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            if (!Advertisement.isSupported)
            {
                Debug.LogError("[AdsManager]: Unity Ads Legacy isn't supported!");

                tcs.SetResult(false);

                return await tcs.Task;
            }

            if (Advertisement.isInitialized)
            {
                Debug.LogError("[AdsManager]: Unity Ads Legacy is already initialized!");

                tcs.SetResult(true);

                return await tcs.Task;
            }

            placementBannerID = GetBannerID();
            placementInterstitialID = GetInterstitialID();
            placementRewardedVideoID = GetRewardedVideoID();
            appId = GetUnityAdsAppID();

            InitGDPR();

            unityAdvertisment = Initializer.GameObject.AddComponent<UnityAdvertismentListener>();
            unityAdvertisment.Init(adsSettings, this, tcs);

            Advertisement.Initialize(appId, Monetization.DebugMode, unityAdvertisment);

            Advertisement.Banner.SetPosition((UnityEngine.Advertisements.BannerPosition)adsSettings.UnityAdsContainer.BannerPosition);

            if (Monetization.VerboseLogging)
            {
                Debug.Log("[AdsManager]: Unity Ads is supported: " + Advertisement.isSupported);
                Debug.Log("[AdsManager]: Unity Ads version: " + Advertisement.version);
            }

            return await tcs.Task;
        }

        /// <summary>
        /// Destroys the currently loaded banner ad by hiding it permanently.
        /// </summary>
        public override void DestroyBanner()
        {
            Advertisement.Banner.Hide(true);
        }

        /// <summary>
        /// Hides the currently loaded banner ad without destroying it.
        /// </summary>
        public override void HideBanner()
        {
            Advertisement.Banner.Hide(false);
        }

        /// <summary>
        /// Requests an interstitial ad from Unity Ads.
        /// </summary>
        public override void RequestInterstitial()
        {
            Advertisement.Load(placementInterstitialID, unityAdvertisment);
        }

        /// <summary>
        /// Requests a rewarded video ad from Unity Ads.
        /// </summary>
        public override void RequestRewardedVideo()
        {
            Advertisement.Load(placementRewardedVideoID, unityAdvertisment);
        }

        /// <summary>
        /// Displays the banner ad.
        /// </summary>
        public override void ShowBanner()
        {
            Advertisement.Banner.Show(placementBannerID);
        }

        /// <summary>
        /// Displays an interstitial ad. Invokes the provided callback after the ad finishes.
        /// </summary>
        /// <param name="callback">Callback to execute after the interstitial ad is shown.</param>
        public override void ShowInterstitial(AdvertisementCallback callback)
        {
            Advertisement.Show(placementInterstitialID, unityAdvertisment);
        }

        /// <summary>
        /// Displays a rewarded video ad. Invokes the provided callback after the ad finishes.
        /// </summary>
        /// <param name="callback">Callback to execute after the rewarded video ad is shown.</param>
        public override void ShowRewardedVideo(AdvertisementCallback callback)
        {
            Advertisement.Show(placementRewardedVideoID, unityAdvertisment);
        }

        /// <summary>
        /// Checks if an interstitial ad is loaded and ready to be shown.
        /// </summary>
        /// <returns>True if an interstitial ad is loaded; otherwise, false.</returns>
        public override bool IsInterstitialLoaded()
        {
            return interstitialIsLoaded;
        }

        /// <summary>
        /// Checks if a rewarded video ad is loaded and ready to be shown.
        /// </summary>
        /// <returns>True if a rewarded video ad is loaded; otherwise, false.</returns>
        public override bool IsRewardedVideoLoaded()
        {
            return rewardVideoIsLoaded;
        }

        /// <summary>
        /// Retrieves the Unity Ads App ID based on the platform (Android or iOS).
        /// </summary>
        /// <returns>The Unity Ads App ID for the platform, or an empty string if the platform is unsupported.</returns>
        public string GetUnityAdsAppID()
        {
#if UNITY_ANDROID
            return adsSettings.UnityAdsContainer.AndroidAppID;
#elif UNITY_IOS
            return adsSettings.UnityAdsContainer.IOSAppID;
#else
            return string.Empty;
#endif
        }

        /// <summary>
        /// Initializes GDPR compliance for Unity Ads by setting user consent via MetaData.
        /// </summary>
        private void InitGDPR()
        {
            bool isConsentGiven = AdsManager.CanRequestAds();
            string gdprState = isConsentGiven ? "true" : "false";

            MetaData gdprMetaData = new MetaData("gdpr");
            gdprMetaData.Set("consent", gdprState);
            Advertisement.SetMetaData(gdprMetaData);
        }

        /// <summary>
        /// Retrieves the banner placement ID based on the platform.
        /// </summary>
        /// <returns>The banner placement ID, or an empty string if the platform is unsupported.</returns>
        public string GetBannerID()
        {
#if UNITY_ANDROID
            return adsSettings.UnityAdsContainer.AndroidBannerID;
#elif UNITY_IOS
            return adsSettings.UnityAdsContainer.IOSBannerID;
#else
            return string.Empty;
#endif
        }

        /// <summary>
        /// Retrieves the interstitial placement ID based on the platform.
        /// </summary>
        /// <returns>The interstitial placement ID, or an empty string if the platform is unsupported.</returns>
        public string GetInterstitialID()
        {
#if UNITY_ANDROID
            return adsSettings.UnityAdsContainer.AndroidInterstitialID;
#elif UNITY_IOS
            return adsSettings.UnityAdsContainer.IOSInterstitialID;
#else
            return string.Empty;
#endif
        }

        /// <summary>
        /// Retrieves the rewarded video placement ID based on the platform.
        /// </summary>
        /// <returns>The rewarded video placement ID, or an empty string if the platform is unsupported.</returns>
        public string GetRewardedVideoID()
        {
#if UNITY_ANDROID
            return adsSettings.UnityAdsContainer.AndroidRewardedVideoID;
#elif UNITY_IOS
            return adsSettings.UnityAdsContainer.IOSRewardedVideoID;
#else
            return string.Empty;
#endif
        }

        /// <summary>
        /// Handles Unity Ads events for loading, showing, and initialization.
        /// Implements necessary listeners for Unity Ads lifecycle.
        /// </summary>
        private class UnityAdvertismentListener : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener, IUnityAdsInitializationListener
        {
            private UnityAdsLegacyHandler adsHandler;
            private AdsSettings adsSettings;
            private TaskCompletionSource<bool> initTask;

            /// <summary>
            /// Initializes the UnityAds listener with necessary settings.
            /// </summary>
            public void Init(AdsSettings adsSettings, UnityAdsLegacyHandler adsHandler, TaskCompletionSource<bool> initTask)
            {
                this.adsSettings = adsSettings;
                this.adsHandler = adsHandler;
                this.initTask = initTask;
            }

            /// <summary>
            /// Called when Unity Ads initialization is complete.
            /// </summary>
            public void OnInitializationComplete()
            {
                initTask.SetResult(true);
            }

            /// <summary>
            /// Called when Unity Ads initialization fails.
            /// Retries initialization up to the defined max attempt limit.
            /// </summary>
            public void OnInitializationFailed(UnityAdsInitializationError error, string message)
            {
                adsHandler.initializationAttemptCount++;

                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: OnInitializationFailed event error:" + error.ToString() + " message: " + message);

                if (adsHandler.initializationAttemptCount <= INIT_CHECK_MAX_ATTEMPT_AMOUNT)
                {
                    Advertisement.Initialize(appId, Monetization.DebugMode, this);
                }
                else
                {
                    if (Monetization.VerboseLogging)
                        Debug.Log("[AdsManager]: OnInitializationFailed in every attempt");

                    initTask.SetResult(false);
                }
            }

            /// <summary>
            /// Called when an ad is successfully loaded.
            /// </summary>
            public void OnUnityAdsAdLoaded(string placementId)
            {
                if (placementId.Equals(placementBannerID))
                {
                    Advertisement.Banner.Show(placementBannerID);

                    if (Monetization.VerboseLogging)
                        Debug.Log("[AdsManager]: OnUnityAdsAdLoaded - banner loaded");
                }
                else if (placementId.Equals(placementInterstitialID))
                {
                    adsHandler.interstitialIsLoaded = true;

                    if (Monetization.VerboseLogging)
                        Debug.Log("[AdsManager]: OnUnityAdsAdLoaded - interstitial loaded");
                }
                else if (placementId.Equals(placementRewardedVideoID))
                {
                    adsHandler.rewardVideoIsLoaded = true;

                    if (Monetization.VerboseLogging)
                        Debug.Log("[AdsManager]: OnUnityAdsAdLoaded - rewardVideo loaded");
                }
            }

            /// <summary>
            /// Called when Unity Ads encounters an error.
            /// </summary>
            public void OnUnityAdsDidError(string message)
            {
                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: OnUnityAdsDidError - " + message);
            }

            /// <summary>
            /// Called when an ad finishes displaying.
            /// Handles callbacks based on ad type (Interstitial or Rewarded).
            /// </summary>
            public void OnUnityAdsDidFinish(string placementId, ShowResult showResult)
            {
                if (placementId == placementInterstitialID)
                {
                    AdsManager.OnProviderAdClosed(AdProvider.UnityAdsLegacy, AdType.Interstitial);

                    AdsManager.ExecuteInterstitialCallback(showResult == ShowResult.Finished);

                    AdsManager.ResetInterstitialDelayTime();
                }
                else if (placementId == placementRewardedVideoID)
                {
                    bool state = showResult == ShowResult.Finished;

                    // Reward the player
                    AdsManager.ExecuteRewardVideoCallback(state);

                    AdsManager.OnProviderAdClosed(AdProvider.UnityAdsLegacy, AdType.RewardedVideo);

                    AdsManager.ResetInterstitialDelayTime();
                }

                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: OnUnityAdsDidFinish - " + placementId + ". Result - " + showResult);
            }

            /// <summary>
            /// Called when an ad starts being displayed.
            /// </summary>
            public void OnUnityAdsDidStart(string placementId)
            {
                if (placementId == placementInterstitialID)
                {
                    AdsManager.OnProviderAdDisplayed(AdProvider.UnityAdsLegacy, AdType.Interstitial);
                }
                else if (placementId == placementRewardedVideoID)
                {
                    AdsManager.OnProviderAdDisplayed(AdProvider.UnityAdsLegacy, AdType.RewardedVideo);
                }

                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: OnUnityAdsDidStart - " + placementId);
            }

            /// <summary>
            /// Called when an ad fails to load.
            /// </summary>
            public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
            {
                if (placementId == placementInterstitialID)
                {
                    adsHandler.interstitialIsLoaded = false;
                }
                else if (placementId == placementRewardedVideoID)
                {
                    adsHandler.rewardVideoIsLoaded = false;
                }

                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: OnUnityAdsFailedToLoad - " + placementId + ". Error - " + error + " .Message: " + message);
            }

            /// <summary>
            /// Called when Unity Ads is ready to show an ad.
            /// </summary>
            public void OnUnityAdsReady(string placementId)
            {
                if (placementId == placementBannerID)
                {
                    AdsManager.OnProviderAdLoaded(AdProvider.UnityAdsLegacy, AdType.Banner);
                }
                else if (placementId == placementInterstitialID)
                {
                    AdsManager.OnProviderAdLoaded(AdProvider.UnityAdsLegacy, AdType.Interstitial);
                }
                else if (placementId == placementRewardedVideoID)
                {
                    AdsManager.OnProviderAdLoaded(AdProvider.UnityAdsLegacy, AdType.RewardedVideo);
                }

                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: OnUnityAdsReady - " + placementId);
            }

            /// <summary>
            /// Called when the ad is clicked.
            /// </summary>
            public void OnUnityAdsShowClick(string placementId)
            {
                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: OnUnityAdsFailedToLoad - " + placementId);
            }

            /// <summary>
            /// Called when an ad is shown completely.
            /// Responsible for resetting load flags and requesting new ads after completion.
            /// </summary>
            public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
            {
                if (placementId == placementInterstitialID)
                {
                    adsHandler.interstitialIsLoaded = false;

                    AdsManager.OnProviderAdClosed(AdProvider.UnityAdsLegacy, AdType.Interstitial);

                    AdsManager.ExecuteInterstitialCallback(showCompletionState == UnityAdsShowCompletionState.COMPLETED);

                    AdsManager.ResetInterstitialDelayTime();

                    AdsManager.RequestInterstitial();
                }
                else if (placementId == placementRewardedVideoID)
                {
                    adsHandler.rewardVideoIsLoaded = false;

                    bool state = showCompletionState == UnityAdsShowCompletionState.COMPLETED;

                    // Reward the player
                    AdsManager.ExecuteRewardVideoCallback(state);

                    AdsManager.OnProviderAdClosed(AdProvider.UnityAdsLegacy, AdType.RewardedVideo);

                    AdsManager.ResetInterstitialDelayTime();

                    AdsManager.RequestRewardBasedVideo();
                }

                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: OnUnityAdsDidFinish - " + placementId + ". Result - " + showCompletionState);
            }

            /// <summary>
            /// Called when there is an error in showing the ad.
            /// If the ad is not ready, requests a new ad.
            /// </summary>
            public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
            {
                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: OnUnityAdsShowFailure - " + placementId + " - " + message);

                if (error == UnityAdsShowError.NOT_READY)
                {
                    if (placementId == placementInterstitialID)
                    {
                        AdsManager.RequestInterstitial();
                    }
                    else if (placementId == placementRewardedVideoID)
                    {
                        AdsManager.RequestRewardBasedVideo();
                    }
                }
            }

            /// <summary>
            /// Called when an ad starts showing.
            /// </summary>
            public void OnUnityAdsShowStart(string placementId)
            {
                if (placementId == placementInterstitialID)
                {
                    AdsManager.OnProviderAdLoaded(AdProvider.UnityAdsLegacy, AdType.Interstitial);
                }
                else if (placementId == placementRewardedVideoID)
                {
                    AdsManager.OnProviderAdLoaded(AdProvider.UnityAdsLegacy, AdType.RewardedVideo);
                }

                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: OnUnityAdsShowStart - " + placementId);
            }
        }
    }
#endif
}
