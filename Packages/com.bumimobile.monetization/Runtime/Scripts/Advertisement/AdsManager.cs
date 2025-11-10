#pragma warning disable 0649
#pragma warning disable 0162

using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace BumiMobile
{
    [StaticUnload]
    [Define("MODULE_ADMOB", "GoogleMobileAds.Api.MobileAds")]
    [Define("MODULE_UNITYADS", "UnityEngine.Advertisements.Advertisement")]
    [Define("MODULE_LEVELPLAY", "IronSource")]
    public static class AdsManager
    {
        private const int INIT_ATTEMPTS_AMOUNT = 30;

        private const string FIRST_LAUNCH_PREFS = "FIRST_LAUNCH";

        private static AdProviderHandler[] AD_PROVIDERS;

        private static bool isModuleInitialized;

        private static AdsSettings settings;
        public static AdsSettings Settings => settings;

        private static double lastInterstitialTime;

        private static AdProviderHandler.AdvertisementCallback rewardedVideoCallback;
        private static AdProviderHandler.AdvertisementCallback interstitalCallback;

        private static List<SimpleCallback> mainThreadEvents = new List<SimpleCallback>();

        private static bool isFirstAdLoaded = false;
        private static bool waitingForRewardVideoCallback;

        private static bool isBannerActive = true;

        private static Coroutine loadingCoroutine;
        private static TweenCase delayTweenCase;

        private static Dictionary<AdProvider, AdProviderHandler> advertisingActiveModules = new Dictionary<AdProvider, AdProviderHandler>();

        // Events
        public static event SimpleCallback ForcedAdDisabled;

        public static event AdsModuleCallback AdProviderInitialized;
        public static event AdsEventsCallback AdLoaded;
        public static event AdsEventsCallback AdDisplayed;
        public static event AdsEventsCallback AdClosed;

        public static AdsBoolCallback InterstitialConditions;

        private static AdSave save;

        private static List<LoadingTask> loadingTasks;

        #region Initialize
        public static void Init(MonetizationSettings monetizationSettings)
        {
            if (isModuleInitialized)
            {
                Debug.LogWarning("[AdsManager]: Module already exists!");

                return;
            }

            isModuleInitialized = true;
            isFirstAdLoaded = false;

            settings = monetizationSettings.AdsSettings;

            save = SaveController.GetSaveObject<AdSave>("advertisement_forced_ad");

            if (settings == null)
            {
                Debug.LogError("[AdsManager]: Settings don't exist!");

                return;
            }

            AD_PROVIDERS = GetProviders();

            if (!PlayerPrefs.HasKey(FIRST_LAUNCH_PREFS))
            {
                lastInterstitialTime = Time.time + settings.InterstitialFirstStartDelay;

                PlayerPrefs.SetInt(FIRST_LAUNCH_PREFS, 1);
            }
            else
            {
                lastInterstitialTime = Time.time + settings.InterstitialStartDelay;
            }

            Initializer.GameObject.AddComponent<AdsManager.AdEventExecutor>();

            advertisingActiveModules = new Dictionary<AdProvider, AdProviderHandler>();
            for (int i = 0; i < AD_PROVIDERS.Length; i++)
            {
                if (IsModuleEnabled(AD_PROVIDERS[i].ProviderType))
                {
                    AD_PROVIDERS[i].LinkSettings(Monetization.Settings);

                    advertisingActiveModules.Add(AD_PROVIDERS[i].ProviderType, AD_PROVIDERS[i]);
                }
            }

            if (Monetization.VerboseLogging)
            {
                if (settings.BannerType != AdProvider.Disable && !advertisingActiveModules.ContainsKey(settings.BannerType))
                    Debug.LogWarning("[AdsManager]: Banner type (" + settings.BannerType + ") is selected, but isn't active!");

                if (settings.InterstitialType != AdProvider.Disable && !advertisingActiveModules.ContainsKey(settings.InterstitialType))
                    Debug.LogWarning("[AdsManager]: Interstitial type (" + settings.InterstitialType + ") is selected, but isn't active!");

                if (settings.RewardedVideoType != AdProvider.Disable && !advertisingActiveModules.ContainsKey(settings.RewardedVideoType))
                    Debug.LogWarning("[AdsManager]: Rewarded Video type (" + settings.RewardedVideoType + ") is selected, but isn't active!");
            }

            loadingTasks = new List<LoadingTask>();

            // Add loading task if GDPR isn't created
            if (settings.IsUMPEnabled)
            {
                UMPLoadingTask cmpLoadingTask = new UMPLoadingTask(monetizationSettings);
                cmpLoadingTask.OnTaskCompleted += LoadingTaskCompleted;

                loadingTasks.Add(cmpLoadingTask);
            }

            if(settings.IsIDFAEnabled)
            {
                IDFALoadingTask idfaTask = new IDFALoadingTask(monetizationSettings);
                idfaTask.OnTaskCompleted += LoadingTaskCompleted;

                loadingTasks.Add(idfaTask);
            }

            if(loadingTasks.IsNullOrEmpty())
            {
                InitializeModules(settings.LoadAdsOnStart);
            }
            else
            {
                // Invoke first loading task
                LoadingTask loadingTask = loadingTasks[0];

                loadingTasks.RemoveAt(0);

                GameLoading.AddTask(loadingTask);
            }
        }

        private static void LoadingTaskCompleted(LoadingTask.CompleteStatus status)
        {
            if (status == LoadingTask.CompleteStatus.Skipped || status == LoadingTask.CompleteStatus.Completed)
            {
                if(!loadingTasks.IsNullOrEmpty())
                {
                    LoadingTask loadingTask = loadingTasks[0];

                    loadingTasks.RemoveAt(0);

                    GameLoading.AddTask(loadingTask);
                }
                else
                {
                    CallEventInMainThread(() =>
                    {
                        InitializeModules(settings.LoadAdsOnStart);
                    });
                }
            }
        }

        private static async void InitializeModules(bool loadAds)
        {
            // Loop through all the providers and initialize them asynchronously
            foreach (AdProviderHandler providerHandler in advertisingActiveModules.Values)
            {
                Debug.Log($"[AdsManager]: {providerHandler.ProviderType} is trying to initialize!");

                bool isInitialized = await providerHandler.InitAsync();

                if (isInitialized)
                {
                    if (Monetization.VerboseLogging)
                        Debug.Log($"[AdsManager]: {providerHandler.ProviderType} initialized successfully.");
                }
                else
                {
                    Debug.LogError($"[AdsManager]: {providerHandler.ProviderType} failed to initialize.");
                }
            }

            if (loadAds)
            {
                TryToLoadFirstAds();
            }
        }
        #endregion

        private static void Update()
        {
            if (!isModuleInitialized)
                return;

            if (mainThreadEvents.Count > 0)
            {
                for (int i = 0; i < mainThreadEvents.Count; i++)
                {
                    mainThreadEvents[i]?.Invoke();
                }

                mainThreadEvents.Clear();
            }

            if (settings.AutoShowInterstitial)
            {
                if (lastInterstitialTime < Time.time)
                {
                    ShowInterstitial(null);

                    ResetInterstitialDelayTime();
                }
            }
        }

        public static void TryToLoadFirstAds()
        {
            if (loadingCoroutine == null)
            {
                Debug.Log("[AdsManager]: Loading first ads..");

                loadingCoroutine = Tween.InvokeCoroutine(TryToLoadAdsCoroutine());
            }
        }

        private static IEnumerator TryToLoadAdsCoroutine()
        {
            int initAttemps = 0;

            yield return new WaitForSeconds(1.0f);

            while (!isFirstAdLoaded || initAttemps > INIT_ATTEMPTS_AMOUNT)
            {
                if (LoadFirstAds())
                    break;

                yield return new WaitForSeconds(1.0f * (initAttemps + 1));

                initAttemps++;
            }

            if (Monetization.VerboseLogging)
                Debug.Log("[AdsManager]: First ads have loaded!");
        }

        private static bool LoadFirstAds()
        {
            if (!isModuleInitialized)
                return false;

            if (isFirstAdLoaded)
                return true;

            if (settings.IsIDFAEnabled && !AdsManager.IsIDFADetermined())
                return false;

            bool isRewardedVideoModuleInititalized = AdsManager.IsModuleInititalized(AdsManager.Settings.RewardedVideoType);
            bool isInterstitialModuleInitialized = AdsManager.IsModuleInititalized(AdsManager.Settings.InterstitialType);
            bool isBannerModuleInitialized = AdsManager.IsModuleInititalized(AdsManager.Settings.BannerType);

            bool isRewardedVideoActive = AdsManager.Settings.RewardedVideoType != AdProvider.Disable;
            bool isInterstitialActive = AdsManager.Settings.InterstitialType != AdProvider.Disable;
            bool isBannerActive = AdsManager.Settings.BannerType != AdProvider.Disable;

            if ((!isRewardedVideoActive || isRewardedVideoModuleInititalized) && (!isInterstitialActive || isInterstitialModuleInitialized) && (!isBannerActive || isBannerModuleInitialized))
            {
                if (isRewardedVideoActive)
                    AdsManager.RequestRewardBasedVideo();

                bool isForcedAdEnabled = AdsManager.IsForcedAdEnabled();
                if (isInterstitialActive && isForcedAdEnabled)
                    AdsManager.RequestInterstitial();

                if (isBannerActive && isForcedAdEnabled)
                    AdsManager.ShowBanner();

                isFirstAdLoaded = true;

                return true;
            }

            return false;
        }

        public static void CallEventInMainThread(SimpleCallback callback)
        {
            if (callback != null)
            {
                mainThreadEvents.Add(callback);
            }
        }

        public static void ShowErrorMessage()
        {
            SystemMessage.ShowMessage("Network error. Please try again later");
        }

        public static bool IsModuleEnabled(AdProvider advertisingModule)
        {
            if (!Monetization.IsActive || !isModuleInitialized)
                return false;

            if (advertisingModule == AdProvider.Disable)
                return false;

            return (Settings.BannerType == advertisingModule || Settings.InterstitialType == advertisingModule || Settings.RewardedVideoType == advertisingModule);
        }

        public static AdProviderHandler GetAdProvider(AdProvider adProvider)
        {
            if(advertisingActiveModules.ContainsKey(adProvider))
            {
                return advertisingActiveModules[adProvider];
            }

            return null;
        }

        public static bool IsModuleActive(AdProvider advertisingModule)
        {
            return advertisingActiveModules.ContainsKey(advertisingModule);
        }

        public static bool IsModuleInititalized(AdProvider advertisingModule)
        {
            if (advertisingActiveModules.ContainsKey(advertisingModule))
            {
                return advertisingActiveModules[advertisingModule].IsInitialized;
            }

            return false;
        }

        #region Interstitial
        public static bool IsInterstitialLoaded()
        {
            if (!Monetization.IsActive || !isModuleInitialized)
            {
                Debug.LogWarning("[IAP Manager]: Mobile monetization is disabled!");

                return false;
            }

            AdProvider advertisingModules = settings.InterstitialType;

            if (!save.IsForcedAdEnabled || !IsModuleActive(advertisingModules))
                return false;

            return advertisingActiveModules[advertisingModules].IsInterstitialLoaded();
        }

        public static void RequestInterstitial()
        {
            if (!Monetization.IsActive || !isModuleInitialized)
            {
                Debug.LogWarning("[IAP Manager]: Mobile monetization is disabled!");

                return;
            }

            AdProvider advertisingModules = settings.InterstitialType;

            if (!save.IsForcedAdEnabled || !IsModuleActive(advertisingModules) || !advertisingActiveModules[advertisingModules].IsInitialized || advertisingActiveModules[advertisingModules].IsInterstitialLoaded())
                return;

            advertisingActiveModules[advertisingModules].RequestInterstitial();
        }

        public static void ShowInterstitial(AdProviderHandler.AdvertisementCallback callback, bool ignoreConditions = false)
        {
            if (!Monetization.IsActive || !isModuleInitialized)
            {
                Debug.LogWarning("[IAP Manager]: Mobile monetization is disabled!");

                ExecuteInterstitialCallback(false);

                return;
            }

            AdProvider advertisingModules = settings.InterstitialType;

            interstitalCallback = callback;

            if (!save.IsForcedAdEnabled || !IsModuleActive(advertisingModules) || (!ignoreConditions && (!CheckInterstitialTime() || !CheckExtraInterstitialCondition())) || !advertisingActiveModules[advertisingModules].IsInitialized || !advertisingActiveModules[advertisingModules].IsInterstitialLoaded())
            {
                ExecuteInterstitialCallback(false);

                return;
            }

            delayTweenCase.KillActive();

            if (settings.LoadingAdDuration > 0)
            {
                SystemMessage.ShowLoadingPanel();
                SystemMessage.ChangeLoadingMessage(settings.LoadingMessage);

                delayTweenCase.KillActive();
                delayTweenCase = Tween.DelayedCall(settings.LoadingAdDuration, () =>
                {
                    advertisingActiveModules[advertisingModules].ShowInterstitial(callback);

                    SystemMessage.HideLoadingPanel();
                });
            }
            else
            {
                advertisingActiveModules[advertisingModules].ShowInterstitial(callback);
            }
        }

        public static void ExecuteInterstitialCallback(bool result)
        {
            if (interstitalCallback != null)
            {
                CallEventInMainThread(() => interstitalCallback.Invoke(result));
            }
        }

        public static void SetInterstitialDelayTime(float time)
        {
            lastInterstitialTime = Time.time + time;
        }

        public static void ResetInterstitialDelayTime()
        {
            lastInterstitialTime = Time.time + settings.InterstitialShowingDelay;
        }

        private static bool CheckInterstitialTime()
        {
            if (Monetization.VerboseLogging)
                Debug.Log("[AdsManager]: Interstitial Time: " + lastInterstitialTime + "; Time: " + Time.time);

            return lastInterstitialTime < Time.time;
        }

        public static bool CheckExtraInterstitialCondition()
        {
            if (InterstitialConditions != null)
            {
                bool state = true;

                System.Delegate[] listDelegates = InterstitialConditions.GetInvocationList();
                for (int i = 0; i < listDelegates.Length; i++)
                {
                    if (!(bool)listDelegates[i].DynamicInvoke())
                    {
                        state = false;

                        break;
                    }
                }

                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: Extra condition interstitial state: " + state);

                return state;
            }

            return true;
        }
        #endregion

        #region Rewarded Video
        public static bool IsRewardBasedVideoLoaded()
        {
            if (!Monetization.IsActive || !isModuleInitialized)
            {
                Debug.LogWarning("[IAP Manager]: Mobile monetization is disabled!");

                return false;
            }

            AdProvider advertisingModule = settings.RewardedVideoType;

            if (!IsModuleActive(advertisingModule) || !advertisingActiveModules[advertisingModule].IsInitialized)
                return false;

            return advertisingActiveModules[advertisingModule].IsRewardedVideoLoaded();
        }

        public static void RequestRewardBasedVideo()
        {
            if (!Monetization.IsActive || !isModuleInitialized)
            {
                Debug.LogWarning("[IAP Manager]: Mobile monetization is disabled!");

                return;
            }

            AdProvider advertisingModule = settings.RewardedVideoType;

            if (!IsModuleActive(advertisingModule) || !advertisingActiveModules[advertisingModule].IsInitialized || advertisingActiveModules[advertisingModule].IsRewardedVideoLoaded())
                return;

            advertisingActiveModules[advertisingModule].RequestRewardedVideo();
        }

        public static void ShowRewardBasedVideo(AdProviderHandler.AdvertisementCallback callback, bool showErrorMessage = true)
        {
            rewardedVideoCallback = callback;
            waitingForRewardVideoCallback = true;

            if (!Monetization.IsActive || !isModuleInitialized)
            {
                Debug.LogWarning("[IAP Manager]: Mobile monetization is disabled!");

                ExecuteRewardVideoCallback(false);

                return;
            }

            AdProvider advertisingModule = settings.RewardedVideoType;
            
            if (!IsModuleActive(advertisingModule) || !advertisingActiveModules[advertisingModule].IsInitialized || !advertisingActiveModules[advertisingModule].IsRewardedVideoLoaded())
            {
                ExecuteRewardVideoCallback(false);

                if (showErrorMessage)
                    ShowErrorMessage();

                return;
            }

            delayTweenCase.KillActive();

            if (settings.LoadingAdDuration > 0)
            {
                SystemMessage.ShowLoadingPanel();
                SystemMessage.ChangeLoadingMessage(settings.LoadingMessage);

                delayTweenCase.KillActive();
                delayTweenCase = Tween.DelayedCall(settings.LoadingAdDuration, () =>
                {
                    advertisingActiveModules[advertisingModule].ShowRewardedVideo(callback);

                    SystemMessage.HideLoadingPanel();
                });
            }
            else
            {
                advertisingActiveModules[advertisingModule].ShowRewardedVideo(callback);
            }
        }

        public static void ExecuteRewardVideoCallback(bool result)
        {
            if (rewardedVideoCallback != null && waitingForRewardVideoCallback)
            {
                CallEventInMainThread(() => rewardedVideoCallback.Invoke(result));

                waitingForRewardVideoCallback = false;

                if (Monetization.VerboseLogging)
                {
                    Debug.Log("[AdsManager]: Reward received: " + result);
                }
            }
        }
        #endregion

        #region Banner
        public static void ShowBanner()
        {
            if (!Monetization.IsActive || !isModuleInitialized)
            {
                Debug.LogWarning("[IAP Manager]: Mobile monetization is disabled!");

                return;
            }

            if (!isBannerActive) return;

            AdProvider advertisingModule = settings.BannerType;
            
            if (!save.IsForcedAdEnabled || !IsModuleActive(advertisingModule) ||
                !advertisingActiveModules[advertisingModule].IsInitialized)
            {
                return;
            }

            advertisingActiveModules[advertisingModule].ShowBanner();
        }

        public static void DestroyBanner()
        {
            if (!Monetization.IsActive || !isModuleInitialized)
            {
                Debug.LogWarning("[IAP Manager]: Mobile monetization is disabled!");

                return;
            }

            AdProvider advertisingModule = settings.BannerType;

            if (!IsModuleActive(advertisingModule) || !advertisingActiveModules[advertisingModule].IsInitialized)
                return;

            advertisingActiveModules[advertisingModule].DestroyBanner();
        }

        public static void HideBanner()
        {
            if (!Monetization.IsActive || !isModuleInitialized)
            {
                Debug.LogWarning("[IAP Manager]: Mobile monetization is disabled!");

                return;
            }

            AdProvider advertisingModule = settings.BannerType;

            if (!IsModuleActive(advertisingModule) || !advertisingActiveModules[advertisingModule].IsInitialized)
                return;

            advertisingActiveModules[advertisingModule].HideBanner();
        }

        public static void EnableBanner()
        {
            if (!Monetization.IsActive || !isModuleInitialized)
            {
                Debug.LogWarning("[IAP Manager]: Mobile monetization is disabled!");

                return;
            }

            isBannerActive = true;

            ShowBanner();
        }

        public static void DisableBanner()
        {
            if (!Monetization.IsActive || !isModuleInitialized)
            {
                Debug.LogWarning("[IAP Manager]: Mobile monetization is disabled!");

                return;
            }

            isBannerActive = false;

            HideBanner();
        }
        #endregion

        #region UMP
        public static bool CanRequestAds()
        {
            if (!settings.IsUMPEnabled)
            {
                if(Monetization.VerboseLogging)
                    Debug.LogWarning("[AdsManager]: UMP is disabled in Monetization Settings!");

                return false;
            }

#if MODULE_ADMOB
            return GoogleMobileAds.Ump.Api.ConsentInformation.CanRequestAds();
#else
            return false;
#endif
        }

        public static ConsentRequirementStatus GetConsentStatus()
        {
            if (!settings.IsUMPEnabled)
            {
                if (Monetization.VerboseLogging)
                    Debug.LogWarning("[AdsManager]: UMP is disabled in Monetization Settings!");

                return ConsentRequirementStatus.Unknown;
            }

#if MODULE_ADMOB
            return (ConsentRequirementStatus)GoogleMobileAds.Ump.Api.ConsentInformation.PrivacyOptionsRequirementStatus;
#else
            return ConsentRequirementStatus.Unknown;
#endif
        }

        public static void ResetConsentState()
        {
            if (!settings.IsUMPEnabled)
            {
                if (Monetization.VerboseLogging)
                    Debug.LogWarning("[AdsManager]: UMP is disabled in Monetization Settings!");

                return;
            }

#if MODULE_ADMOB
            GoogleMobileAds.Ump.Api.ConsentInformation.Reset();
#endif
        }
#endregion

        #region Forced Ad
        public static bool IsForcedAdEnabled()
        {
            return save.IsForcedAdEnabled;
        }

        public static void DisableForcedAd()
        {
            if (!save.IsForcedAdEnabled) return;

            Debug.Log("[Ads Manager]: Banners and interstitials are disabled!");

            save.IsForcedAdEnabled = false;

            NotchSaveArea.Refresh(true);

            ForcedAdDisabled?.Invoke();

            DestroyBanner();
        }
        #endregion

        #region IDFA
        public static AuthorizationTrackingStatus GetIDFAStatus()
        {
            if (!settings.IsIDFAEnabled)
                return AuthorizationTrackingStatus.NOT_DETERMINED;

#if UNITY_IOS && MODULE_IDFA
            return (AuthorizationTrackingStatus)Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
#else
            return AuthorizationTrackingStatus.NOT_DETERMINED;
#endif
        }

        public static bool IsIDFADetermined()
        {
            if (!settings.IsIDFAEnabled)
                return true;

#if UNITY_IOS && MODULE_IDFA
            return Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus() != Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED;
#else
            return true;
#endif
        }
        #endregion

        public static void OnProviderInitialized(AdProvider advertisingModule)
        {
            AdProviderInitialized?.Invoke(advertisingModule);
        }

        public static void OnProviderAdLoaded(AdProvider advertisingModule, AdType advertisingType)
        {
            AdLoaded?.Invoke(advertisingModule, advertisingType);
        }

        public static void OnProviderAdDisplayed(AdProvider advertisingModule, AdType advertisingType)
        {
            AdDisplayed?.Invoke(advertisingModule, advertisingType);

            if (advertisingType == AdType.Interstitial || advertisingType == AdType.RewardedVideo)
            {
                ResetInterstitialDelayTime();
            }
        }

        public static void OnProviderAdClosed(AdProvider advertisingModule, AdType advertisingType)
        {
            AdClosed?.Invoke(advertisingModule, advertisingType);

            if (advertisingType == AdType.Interstitial || advertisingType == AdType.RewardedVideo)
            {
                ResetInterstitialDelayTime();
            }
        }

        private static AdProviderHandler[] GetProviders()
        {
            return new AdProviderHandler[]
            {
                new AdDummyHandler(AdProvider.Dummy), 

#if MODULE_ADMOB
                new AdMobHandler(AdProvider.AdMob), 
#endif

#if MODULE_UNITYADS
                new UnityAdsLegacyHandler(AdProvider.UnityAdsLegacy), 
#endif

#if MODULE_LEVELPLAY
                new LevelPlayHandler(AdProvider.LevelPlay),
#endif
            };
        }

        private static void UnloadStatic()
        {
            isModuleInitialized = false;

            settings = null;
            lastInterstitialTime = 0;

            rewardedVideoCallback = null;
            interstitalCallback = null;

            mainThreadEvents.Clear();

            isFirstAdLoaded = false;
            waitingForRewardVideoCallback = false;

            isBannerActive = true;

            loadingCoroutine = null;

            advertisingActiveModules.Clear();

            ForcedAdDisabled = null;

            AdProviderInitialized = null;
            AdLoaded = null;
            AdDisplayed = null;
            AdClosed = null;

            InterstitialConditions = null;

            save = null;

            loadingTasks = null;

            AD_PROVIDERS = null;
        }

        public delegate void AdsModuleCallback(AdProvider advertisingModules);
        public delegate void AdsEventsCallback(AdProvider advertisingModules, AdType advertisingType);
        public delegate bool AdsBoolCallback();

        private class AdEventExecutor : MonoBehaviour
        {
            private void Update()
            {
                AdsManager.Update();
            }
        }
    }
}

// -----------------
// Advertisement v1.4.2
// -----------------

// Changelog
// v1.4.2
// • Added EnableBanner, DisableBanner methods
// v1.4.1
// • Added ironSource (Unity LevelPlay) ad provider
// v1.4
// • Admob v9.0.0 support
// • Better naming and code cleanup
// • Ads callbacks replaced with simplified ones (AdLoaded, AdDisplayed, AdClosed)
// • Removed ShowInterstitial, ShowRewardedVideo, ShowBanner methods with provider type parameter
// • Added optional bool parameter to ShowInterstitial method. Allows to show interstitial even if conditions aren't met
// v1.3
// • Admob v8.1.0 support
// • Removed IronSource provider
// v1.2.1
// • Some fixes in IronSourse provider
// • Some fixes in Admob provider
// • New interface in Admob provider
// • Added Build Preprocessing for Admob 
// v1.2
// • Added IronSource provider
// v1.1f3
// • GDPR style rework
// • Rewarded video error message
// • Removed GDPR check in AdMob module
// v1.1f2
// • GDPR init bug fixed
// v1.1
// • Added first ad loader
// • Moved IAP check to AdsManager script
// v1.0
// • Added documentation
// v0.3
// • Unity Ads fixed
// v0.2
// • Bug fix
// v0.1
// • Added basic version