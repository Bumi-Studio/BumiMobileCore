using UnityEngine;

namespace BumiMobile
{
    [System.Serializable]
    public class Currency
    {
        [SerializeField] CurrencyType currencyType;
        public CurrencyType CurrencyType => currencyType;

        [SerializeField] int defaultAmount = 0;
        public int DefaultAmount => defaultAmount;

        [SerializeField] Sprite icon;
        public Sprite Icon => icon;

        [SerializeField] CurrencyData data;
        public CurrencyData Data => data;

        [SerializeField] FloatingCloudCase floatingCloud;
        public FloatingCloudCase FloatingCloud => floatingCloud;

    public int Amount { get; set; }

    public string AmountFormatted => CurrencyHelper.Format(Amount);

        public event CurrencyCallback OnCurrencyChanged;

        public void Init()
        {
            data.Init(this);
        }

        public void InvokeChangeEvent(int difference)
        {
            OnCurrencyChanged?.Invoke(this, difference);
        }


        [System.Serializable]
        public class FloatingCloudCase
        {
            [SerializeField] bool addToCloud;
            public bool AddToCloud => addToCloud;

            [SerializeField] float radius = 200;
            public float Radius => radius;

            [SerializeField] GameObject specialPrefab;
            public GameObject SpecialPrefab => specialPrefab;

            [SerializeField] AudioClip appearAudioClip;
            public AudioClip AppearAudioClip => appearAudioClip;

            [SerializeField] AudioClip collectAudioClip;
            public AudioClip CollectAudioClip => collectAudioClip;
        }
    }
}