using UnityEngine;

namespace BumiMobile
{
    [System.Serializable]
    public class AdMobContainer
    {
        public static readonly string ANDROID_BANNER_TEST_ID = "ca-app-pub-3940256099942544/6300978111";
        public static readonly string IOS_BANNER_TEST_ID = "ca-app-pub-3940256099942544/2934735716";
        public static readonly string ANDROID_INTERSTITIAL_TEST_ID = "ca-app-pub-3940256099942544/1033173712";
        public static readonly string IOS_INTERSTITIAL_TEST_ID = "ca-app-pub-3940256099942544/4411468910";
        public static readonly string ANDROID_REWARDED_VIDEO_TEST_ID = "ca-app-pub-3940256099942544/5224354917";
        public static readonly string IOS_REWARDED_VIDEO_TEST_ID = "ca-app-pub-3940256099942544/1712485313";
        public static readonly string ANDROID_OPEN_TEST_ID = "ca-app-pub-3940256099942544/9257395921";
        public static readonly string IOS_OPEN_TEST_ID = "ca-app-pub-3940256099942544/5575463023";

        [SerializeField] string androidAppId;
        [SerializeField] string iosAppId;

        [SerializeField] string androidBannerID = ANDROID_BANNER_TEST_ID;
        public string AndroidBannerID => androidBannerID;
        [SerializeField] string iOSBannerID = IOS_BANNER_TEST_ID;
        public string IOSBannerID => iOSBannerID;

        [SerializeField] string androidInterstitialID = ANDROID_INTERSTITIAL_TEST_ID;
        public string AndroidInterstitialID => androidInterstitialID;
        [SerializeField] string iOSInterstitialID = IOS_INTERSTITIAL_TEST_ID;
        public string IOSInterstitialID => iOSInterstitialID;

        [SerializeField] string androidRewardedVideoID = ANDROID_REWARDED_VIDEO_TEST_ID;
        public string AndroidRewardedVideoID => androidRewardedVideoID;
        [SerializeField] string iOSRewardedVideoID = IOS_REWARDED_VIDEO_TEST_ID;
        public string IOSRewardedVideoID => iOSRewardedVideoID;

        [SerializeField] bool useAppOpenAd = false;
        public bool UseAppOpenAd => useAppOpenAd;

        [SerializeField] string androidAppOpenAdID = ANDROID_OPEN_TEST_ID;
        public string AndroidAppOpenAdID => androidAppOpenAdID;
        [SerializeField] string iOSAppOpenAdID = IOS_OPEN_TEST_ID;
        public string IOSAppOpenAdID => iOSAppOpenAdID;

        [Space]
        [SerializeField] int appOpenAdExpirationHoursTime = 4;
        public int AppOpenAdExpirationHoursTime => appOpenAdExpirationHoursTime;

        [SerializeField] BannerPlacementType bannerType = BannerPlacementType.Banner;
        public BannerPlacementType BannerType => bannerType;

        [SerializeField] BannerPosition bannerPosition = BannerPosition.Bottom;
        public BannerPosition BannerPosition => bannerPosition;

        public enum BannerPlacementType
        {
            Banner = 0,
            MediumRectangle = 1,
            IABBanner = 2,
            Leaderboard = 3,
        }
    }
}
