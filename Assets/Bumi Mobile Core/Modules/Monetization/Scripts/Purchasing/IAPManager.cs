using System.Collections.Generic;
using UnityEngine;

#if MODULE_IAP
using UnityEngine.Purchasing;
#endif

namespace BumiMobile
{
    [StaticUnload]
    public static class IAPManager
    {
        private static Dictionary<ProductKeyType, IAPItem> productsTypeToProductLink;

        public static bool IsInitialized { get; private set; } = false;

        private static IAPWrapper wrapper;

        public static event SimpleCallback Initialized;
        public static event ProductCallback PurchaseCompleted;
        public static event ProductFailCallback PurchaseFailed;

        private static IAPSettings settings;

        public static void Init(MonetizationSettings monetizationSettings)
        {
            if (IsInitialized)
            {
                Debug.LogError("[IAP Manager]: Module is already initialized!");
                return;
            }

            settings = monetizationSettings?.IAPSettings;
            if (settings == null)
            {
                Debug.LogError("[IAP Manager]: IAPSettings is null!");
                return;
            }

            productsTypeToProductLink = new Dictionary<ProductKeyType, IAPItem>();

            IAPItem[] items = settings.StoreItems;
            if (items != null)
            {
                foreach (IAPItem item in items)
                {
                    if (!productsTypeToProductLink.ContainsKey(item.ProductKeyType))
                    {
                        productsTypeToProductLink.Add(item.ProductKeyType, item);
                    }
                    else
                    {
                        Debug.LogError($"[IAP Manager]: Product with the type {item.ProductKeyType} has duplicates in the list!", settings);
                    }
                }
            }

            wrapper = GetPlatformWrapper();
            wrapper.Init(settings);
        }

        public static IAPItem GetIAPItem(string productID)
        {
            if (string.IsNullOrEmpty(productID)) return null;

            foreach (IAPItem item in productsTypeToProductLink.Values)
            {
                if (item.ID == productID)
                    return item;
            }

            return null;
        }

        public static IAPItem GetIAPItem(ProductKeyType productKeyType)
        {
            productsTypeToProductLink.TryGetValue(productKeyType, out IAPItem item);

            return item;
        }

#if MODULE_IAP
        public static Product GetProduct(ProductKeyType productKeyType)
        {
            var iapItem = GetIAPItem(productKeyType);
            return iapItem != null ? UnityIAPWrapper.Controller.products.WithID(iapItem.ID) : null;
        }
#endif

        public static void RestorePurchases()
        {
            if (!Monetization.IsActive || !IsInitialized) return;

            wrapper.RestorePurchases();
        }

        public static void SubscribeOnPurchaseModuleInitted(SimpleCallback callback)
        {
            if (IsInitialized)
            {
                callback?.Invoke();
            }
            else
            {
                Initialized += callback;
            }
        }

        public static void BuyProduct(ProductKeyType productKeyType)
        {
            if (!Monetization.IsActive)
            {
                Debug.LogWarning("[IAP Manager]: Mobile monetization is disabled!", settings);
                return;
            }

            if (!IsInitialized)
            {
                Debug.LogWarning("[IAP Manager]: The module is not initialized!", settings);
                return;
            }

            wrapper.BuyProduct(productKeyType);
        }

        public static ProductData GetProductData(ProductKeyType productKeyType)
        {
            if (!Monetization.IsActive || !IsInitialized) return new ProductData();

            var product = wrapper.GetProductData(productKeyType);

            if (product == null)
            {
                Debug.LogWarning($"[IAP Manager]: Product of type '{productKeyType}' was not found in Monetization Settings. Please ensure it is added to the products list.", settings);
            }

            return product;
        }

        public static bool IsSubscribed(ProductKeyType productKeyType)
        {
            if (!Monetization.IsActive || !IsInitialized) return false;

            return wrapper.IsSubscribed(productKeyType);
        }

        public static string GetProductLocalPriceString(ProductKeyType productKeyType)
        {
            var product = GetProductData(productKeyType);

            if (product == null)
            {
                Debug.LogWarning($"[IAP Manager]: Product of type '{productKeyType}' was not found in Monetization Settings. Please ensure it is added to the products list.", settings);
                return string.Empty;
            }

            return $"{product.ISOCurrencyCode} {product.Price}";
        }

        public static void OnModuleInitialized()
        {
            IsInitialized = true;

            Initialized?.Invoke();

            if (Monetization.VerboseLogging)
                Debug.Log("[IAPManager]: Module is initialized!");
        }

        public static void OnPurchaseCompleted(ProductKeyType productKey)
        {
            PurchaseCompleted?.Invoke(productKey);
        }

        public static void OnPurchaseFailed(ProductKeyType productKey, PurchaseFailureReason failureReason)
        {
            PurchaseFailed?.Invoke(productKey, failureReason);
        }

        private static IAPWrapper GetPlatformWrapper()
        {
#if MODULE_IAP
            return new DummyIAPWrapper();
#else
            return new UnityIAPWrapper();
#endif
        }

        private static void UnloadStatic()
        {
            IsInitialized = false;

            productsTypeToProductLink = null;
            wrapper = null;
            settings = null;

            Initialized = null;
            PurchaseCompleted = null;
            PurchaseFailed = null;
        }

        public delegate void ProductCallback(ProductKeyType productKeyType);
        public delegate void ProductFailCallback(ProductKeyType productKeyType, PurchaseFailureReason failureReason);
    }
}
