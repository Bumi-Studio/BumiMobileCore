using UnityEngine;

namespace BumiMobile
{
    public class SkinReward : Reward
    {
        [SkinPicker]
        [SerializeField] string skinID;

        [SerializeField] bool disableIfSkinIsUnlocked;

        private SkinController skinsController;

        private void Start()
        {
            skinsController = SkinController.Instance;
        }

        private void OnEnable()
        {
            SkinController.SkinUnlocked += OnSkinUnlocked;    
        }

        private void OnDisable()
        {
            SkinController.SkinUnlocked -= OnSkinUnlocked;
        }

        public override void ApplyReward()
        {
            skinsController.UnlockSkin(skinID, true);
        }

        public override bool CheckDisableState()
        {
            if(disableIfSkinIsUnlocked)
            {
                return skinsController.IsSkinUnlocked(skinID);
            }

            return false;
        }

        private void OnSkinUnlocked(ISkinData skinData)
        {
            if(disableIfSkinIsUnlocked)
            {
                if(skinData.ID == skinID)
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
