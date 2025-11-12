using UnityEngine;
using UnityEngine.UI;

namespace BumiMobile
{
    public sealed class AdsRewardsHolder : RewardsHolder
    {
        [Group("Settings"), UniqueID]
        [SerializeField] string rewardID;

        [Group("Settings"), Space]
        [SerializeField] Button adsButton;

        [Group("Settings")]
        [SerializeField] bool disableAfterPurchase;

#if MODULE_SAVE
    private SimpleBoolSave save;
#else
    private bool hasReceivedReward;
#endif

        private void Awake()
        {
            InitializeComponents();

#if MODULE_SAVE
            save = SaveController.GetSaveObject<SimpleBoolSave>($"CurrencyProduct_{rewardID}");
            bool rewardClaimed = save.Value;
#else
            bool rewardClaimed = hasReceivedReward;
#endif

            if (disableAfterPurchase && rewardClaimed)
            {
                // Disable holder game object
                gameObject.SetActive(false);

                return;
            }

            // Check if holder needs to be disabled
            for (int i = 0; i < rewards.Length; i++)
            {
                if (rewards[i].CheckDisableState())
                {
                    // Disable holder game object
                    gameObject.SetActive(false);

                    return;
                }
            }

            adsButton.onClick.AddListener(OnPurchased);
        }

        private void OnPurchased()
        {
#if MODULE_HAPTIC
            Haptic.Play(Haptic.HAPTIC_LIGHT);
#endif

#if MODULE_AUDIO
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
#endif

            AdsManager.ShowRewardBasedVideo((reward) =>
            {
                if (reward)
                {
                    ApplyRewards();

                    bool wasClaimed = true;

                    if (disableAfterPurchase)
                    {
                        // Disable holder game object
                        gameObject.SetActive(false);
                    }

#if MODULE_SAVE
                    save.Value = wasClaimed;
                    SaveController.MarkAsSaveIsRequired();
#else
                    hasReceivedReward = wasClaimed;
#endif
                }
            });
        }
    }
}
