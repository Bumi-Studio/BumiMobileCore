using UnityEngine;

namespace BumiMobile
{
    [System.Serializable]
    public class CurrencyPrice
    {
        private const string TEXT_FORMAT = "<sprite name={0}>{1}";

        [SerializeField] CurrencyType currencyType;
        public CurrencyType CurrencyType => currencyType;

        [SerializeField] int price;
        public int Price => price;

        public Currency Currency => CurrencyController.GetCurrency(currencyType);
        public string FormattedPrice => CurrencyHelper.Format(price);

        public CurrencyPrice()
        {

        }

        public CurrencyPrice(CurrencyType currencyType, int price)
        {
            this.currencyType = currencyType;
            this.price = price;
        }

        public bool EnoughMoneyOnBalance()
        {
            return CurrencyController.HasAmount(currencyType, price);
        }

        public void SubstractFromBalance()
        {
            CurrencyController.Substract(currencyType, price);
        }

        public string GetTextWithIcon()
        {
            return string.Format(TEXT_FORMAT, currencyType, price);
        }
    }
}
