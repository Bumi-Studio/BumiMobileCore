using UnityEngine;

namespace BumiMobile
{
    [System.Serializable]
    public class LevelPlayContainer
    {
        public static readonly string ANDROID_BANNER_TEST_ID = "thnfvcsog13bhn08";
        public static readonly string IOS_BANNER_TEST_ID = "iep3rxsyp9na3rw8";
        public static readonly string ANDROID_INTERSTITIAL_TEST_ID = "aeyqi3vqlv6o8sh9";
        public static readonly string IOS_INTERSTITIAL_TEST_ID = "wmgt0712uuux8ju4";
        public static readonly string ANDROID_OPEN_TEST_ID = "85460dcd";
        public static readonly string IOS_OPEN_TEST_ID = "8545d445";

        [SerializeField] string androidAppKey = ANDROID_OPEN_TEST_ID;
        public string AndroidAppKey => androidAppKey;
        [SerializeField] string iOSAppKey = IOS_OPEN_TEST_ID;
        public string IOSAppKey => iOSAppKey;

        [Space]
        [SerializeField] string androidBannerID = ANDROID_BANNER_TEST_ID;
        public string AndroidBannerID => androidBannerID;
        [SerializeField] string iOSBannerID = IOS_BANNER_TEST_ID;
        public string IOSBannerID => iOSBannerID;

        [Space]
        [SerializeField] string androidInterstitialID = ANDROID_INTERSTITIAL_TEST_ID;
        public string AndroidInterstitialID => androidInterstitialID;
        [SerializeField] string iOSInterstitialID = IOS_INTERSTITIAL_TEST_ID;
        public string IOSInterstitialID => iOSInterstitialID;

        [Space]
        [SerializeField] BannerPosition bannerPosition;
        public BannerPosition BannerPosition => bannerPosition;
        [SerializeField] BannerPlacementType bannerType;
        public BannerPlacementType BannerType => bannerType;

        public enum BannerPlacementType
        {
            Banner = 0,
            Large = 1,
            Rectangle = 2,
            Leaderboard = 3
        }
    }
}