#pragma warning disable 0649

using UnityEngine;

namespace BumiMobile
{
    [System.Serializable]
    public class AdDummyContainer
    {
        [SerializeField] BannerPosition bannerPosition = BannerPosition.Bottom;
        public BannerPosition BannerPosition => bannerPosition;
    }
}