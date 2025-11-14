using System.Threading.Tasks;

namespace BumiMobile
{
    public abstract class IAPWrapper
    {
        public abstract Task Init(IAPSettings settings);
        public abstract void RestorePurchases();
        public abstract void BuyProduct(ProductKeyType productKeyType);
        public abstract ProductData GetProductData(ProductKeyType productKeyType);
        public abstract bool IsSubscribed(ProductKeyType productKeyType);
    }
}