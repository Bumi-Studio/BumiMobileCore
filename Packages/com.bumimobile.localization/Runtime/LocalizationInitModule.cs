using UnityEngine;

namespace BumiMobile
{
    [RegisterModule("Localization Module", core: true, order: 998)]
    public class LocalizationInitModule : InitModule
    {
        public override string ModuleName =>  "Localization";
        [SerializeField]
        private LocalizationSettings localizationSettings;
        
        public override void CreateComponent()
        {
            LocalizationController.Init(localizationSettings);
        }
    }
}
