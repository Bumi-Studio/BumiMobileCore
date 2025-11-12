using UnityEngine;
using UnityEngine.UI;

namespace BumiMobile
{
    public class CurrencyReward : Reward
    {
        [SerializeField] CurrencyData[] currencies;

        [SerializeField] bool spawnCurrencyCloud;

        [ShowIf("spawnCurrencyCloud")]
        [SerializeField] CurrencyType currencyCloudType;
        [ShowIf("spawnCurrencyCloud")]
        [SerializeField] int cloudElementsAmount = 10;
        [ShowIf("spawnCurrencyCloud")]
        [SerializeField] RectTransform currencyCloudSpawnPoint;
        [ShowIf("spawnCurrencyCloud")]
        [SerializeField] RectTransform currencyCloudTargetPoint;

        public override void Init()
        {
            foreach (CurrencyData currencyData in currencies)
            {
                Currency currency = CurrencyController.GetCurrency(currencyData.CurrencyType);

                if (currencyData.CurrencyImage != null)
                    currencyData.CurrencyImage.sprite = currency.Icon;

                currencyData.UpdateTextDisplay();
            }
        }

        public override void ApplyReward()
        {
            void ApplyCurrency()
            {
                foreach (CurrencyData currencyData in currencies)
                {
                    CurrencyController.Add(currencyData.CurrencyType, currencyData.Amount);
                }
            }

            ApplyCurrency();
        }

        [System.Serializable]
        public class CurrencyData
        {
            [SerializeField] CurrencyType currencyType;
            public CurrencyType CurrencyType => currencyType;

            [SerializeField] int amount;
            public int Amount => amount;

            [Space]
            [SerializeField] Image currencyImage;
            public Image CurrencyImage => currencyImage;

            [SerializeField] Text amountText;
            public Text AmountText => amountText;

            [SerializeField] string textFormating = "x{0}";
            public string TextFormating => textFormating;

            [SerializeField] bool formatTheNumber;
            public bool FormatTheNumber => formatTheNumber;

            public void UpdateTextDisplay()
            {
                if (amountText == null) return;

                string numberText = formatTheNumber ? CurrencyHelper.Format(amount) : amount.ToString();
                amountText.text = string.Format(string.IsNullOrEmpty(textFormating) ? "{0}" : textFormating, numberText);
            }
        }
    }
}
