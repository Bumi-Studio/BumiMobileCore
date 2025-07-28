using UnityEngine;

namespace BumiMobile
{
    public class IAPSettings : ScriptableObject
    {
        [SerializeField, Hide] IAPItem[] storeItems;
        public IAPItem[] StoreItems => storeItems;
    }
}