#pragma warning disable 0414

using UnityEngine;

namespace BumiMobile
{
    [HelpURL("https://www.notion.so/wmelongames/Advertisement-221053e32d4047bb880275027daba9f0?pvs=4")]
    public class AdsSettings : ScriptableObject
    {
        [BoxGroup("Advertisement", "Advertisement")]
        [SerializeField] AdProvider bannerType = AdProvider.Dummy;
        public AdProvider BannerType => bannerType;

        [BoxGroup("Advertisement")]
        [SerializeField] AdProvider interstitialType = AdProvider.Dummy;
        public AdProvider InterstitialType => interstitialType;

        [BoxGroup("Advertisement")]
        [SerializeField] AdProvider rewardedVideoType = AdProvider.Dummy;
        public AdProvider RewardedVideoType => rewardedVideoType;

        [BoxGroup("Settings", "Settings")]
        [SerializeField] bool loadAdsOnStart = true;
        public bool LoadAdsOnStart => loadAdsOnStart;

        [Space]
        [BoxGroup("Settings/Interstitial")]
        [Tooltip("Delay in seconds before interstitial appearings on first game launch.")]
        [SerializeField] float interstitialFirstStartDelay = 40f;
        public float InterstitialFirstStartDelay => interstitialFirstStartDelay;

        [BoxGroup("Settings/Interstitial")]
        [Tooltip("Delay in seconds before interstitial appearings.")]
        [SerializeField] float interstitialStartDelay = 40f;
        public float InterstitialStartDelay => interstitialStartDelay;

        [BoxGroup("Settings/Interstitial")]
        [Tooltip("Delay in seconds between interstitial appearings.")]
        [SerializeField] float interstitialShowingDelay = 30f;
        public float InterstitialShowingDelay => interstitialShowingDelay;

        [BoxGroup("Settings/Interstitial")]
        [SerializeField] bool autoShowInterstitial;
        public bool AutoShowInterstitial => autoShowInterstitial;

        [BoxGroup("Settings/Delay")]
        [SerializeField] float loadingAdDuration = 0f;
        public float LoadingAdDuration => loadingAdDuration;

        [BoxGroup("Settings/Delay")]
        [SerializeField] string loadingMessage = "Ad is loading..";
        public string LoadingMessage => loadingMessage;

        [BoxGroup("UMP", "UMP")]
        [SerializeField] bool isUMPEnabled = true;
        public bool IsUMPEnabled => isUMPEnabled;

        [BoxGroup("UMP")]
        [ShowIf("isUMPEnabled")]
        [Tooltip("Set TagForUnderAgeOfConsent (TFUA) to indicate whether a user is under the age of consent. Consent is not requested from the user when TFUA is set to true. Mixed audience apps should set this parameter for child users to ensure consent is not requested.")]
        [SerializeField] bool umpTagForUnderAgeOfConsent = false;
        public bool UMPTagForUnderAgeOfConsent => umpTagForUnderAgeOfConsent;

        [Space]
        [BoxGroup("UMP")]
        [ShowIf("isUMPEnabled")]
        [SerializeField] bool umpDebugMode = false;
        public bool UMPDebugMode => umpDebugMode;

        [BoxGroup("UMP")]
        [ShowIf("isUMPEnabled")]
        [SerializeField] DebugGeography umpDebugGeography;
        public DebugGeography UMPDebugGeography => umpDebugGeography;

        [BoxGroup("IDFA", "IDFA")]
        [SerializeField] bool isIDFAEnabled = false;
        public bool IsIDFAEnabled => isIDFAEnabled;

        [BoxGroup("IDFA")]
        [ShowIf("isIDFAEnabled")]
        [SerializeField] string trackingDescription = "Your data will be used to deliver personalized ads to you.";
        public string TrackingDescription => trackingDescription;

        // Providers
        [SerializeField, Hide] AdMobContainer adMobContainer;
        public AdMobContainer AdMobContainer => adMobContainer;

        [SerializeField, Hide] UnityAdsLegacyContainer unityAdsContainer;
        public UnityAdsLegacyContainer UnityAdsContainer => unityAdsContainer;

        [SerializeField, Hide] LevelPlayContainer levelPlayContainer;
        public LevelPlayContainer LevelPlayContainer => levelPlayContainer;

        [SerializeField, Hide] AdDummyContainer dummyContainer;
        public AdDummyContainer DummyContainer => dummyContainer;

        public bool IsDummyEnabled()
        {
            if (bannerType == AdProvider.Dummy)
                return true;

            if (interstitialType == AdProvider.Dummy)
                return true;

            if (rewardedVideoType == AdProvider.Dummy)
                return true;

            return false;
        }
    }
}