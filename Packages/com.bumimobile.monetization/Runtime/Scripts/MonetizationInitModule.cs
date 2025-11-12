using UnityEngine;

namespace BumiMobile
{
    [RegisterModule("Monetization")]
    public class MonetizationInitModule : InitModule
    {
        public override string ModuleName => "Monetization"; 

        [SerializeField] MonetizationSettings settings;

        public override void CreateComponent()
        {
            Monetization.Init(settings);

            AdsManager.Init(settings);
            IAPManager.Init(settings);
        }
    }
}