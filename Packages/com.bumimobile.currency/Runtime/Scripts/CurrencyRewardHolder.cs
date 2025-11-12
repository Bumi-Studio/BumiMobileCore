using UnityEngine;

namespace BumiMobile
{
    public sealed class CurrencyRewardsHolder : RewardsHolder
    {
        [Group("Settings"), UniqueID]
        [SerializeField] string rewardID;

        [Group("Settings")]
        [SerializeField] UICurrencyButton currencyButton;

        [Group("Settings")]
        [SerializeField] CurrencyPrice price;

        [Group("Settings"), Space]
        [SerializeField] bool disableAfterPurchase;

    private bool isPurchased;

        private void Awake()
        {
            InitializeComponents();

            if(disableAfterPurchase && isPurchased)
            {
                // Disable offer game object
                gameObject.SetActive(false);

                return;
            }

            // Check if offer needs to be disabled
            for (int i = 0; i < rewards.Length; i++)
            {
                if (rewards[i].CheckDisableState())
                {
                    // Disable offer game object
                    gameObject.SetActive(false);

                    return;
                }
            }

            currencyButton.Init(price.Price, price.CurrencyType);
            currencyButton.Purchased += OnPurchased;
        }

        private void OnPurchased()
        {
            ApplyRewards();

            isPurchased = true;

            if(disableAfterPurchase)
            {
                // Disable holder game object
                gameObject.SetActive(false);
            }
        }
    }
}
