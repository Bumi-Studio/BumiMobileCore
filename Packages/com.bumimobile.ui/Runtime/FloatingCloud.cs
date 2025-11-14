using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace BumiMobile
{
    [System.Serializable]
    public class FloatingCloud
    {
        [SerializeField] Data[] floatingCloudCases;

        private static Dictionary<int, Data> floatingCloudLink = new Dictionary<int, Data>();
        private static List<Animation> activeClouds = new List<Animation>();

        public void Init()
        {
            for (int i = 0; i < floatingCloudCases.Length; i++)
            {
                RegisterCase(floatingCloudCases[i]);
            }

#if MODULE_CURRENCY
            Currency[] currencies = CurrencyController.Currencies;
            if(!currencies.IsNullOrEmpty())
            {
                foreach (var currency in currencies)
                {
                    Currency.FloatingCloudCase floatingCloudCase = currency.FloatingCloud;
                    if (floatingCloudCase.AddToCloud)
                    {
                        FloatingCloudSettings floatingCloudSettings;

                        if (floatingCloudCase.SpecialPrefab != null)
                        {
                            floatingCloudSettings = new FloatingCloudSettings(currency.CurrencyType.ToString(), floatingCloudCase.SpecialPrefab);
                        }
                        else
                        {
                            floatingCloudSettings = new FloatingCloudSettings(currency.CurrencyType.ToString(), currency.Icon, new Vector2(100, 100));
                        }

                        floatingCloudSettings.SetAudio(floatingCloudCase.AppearAudioClip, floatingCloudCase.CollectAudioClip);

                        RegisterCase(floatingCloudSettings);
                    }
                }
            }
#endif
        }

        public static void Clear()
        {
            Unload();

            foreach (Data floatingCloudData in floatingCloudLink.Values)
            {
                floatingCloudData.Destroy();
            }

            floatingCloudLink.Clear();
        }

        public static void Unload()
        {
            for (int i = 0; i < activeClouds.Count; i++)
            {
                activeClouds[i].Clear();
            }

            activeClouds.Clear();
        }

        public static void RegisterCase(FloatingCloudSettings floatingCloudSettings)
        {
            int cloudHash = floatingCloudSettings.Name.GetHashCode();

            if (floatingCloudLink.ContainsKey(cloudHash))
            {
                Debug.LogError($"Cloud {floatingCloudSettings.Name} already registered!");

                return;
            }

            Data floatingCloudCase = new Data(floatingCloudSettings);
            floatingCloudCase.Init();

            floatingCloudLink.Add(cloudHash, floatingCloudCase);
        }

        public static void RegisterCase(Data floatingCloudCase)
        {
            int cloudHash = floatingCloudCase.Name.GetHashCode();

            if (floatingCloudLink.ContainsKey(cloudHash))
            {
                Debug.LogError($"Cloud {floatingCloudCase.Name} already registered!");

                return;
            }

            floatingCloudCase.Init();

            floatingCloudLink.Add(cloudHash, floatingCloudCase);
        }

        public static void SpawnCurrency(string key, RectTransform rectTransform, RectTransform targetTransform, int elementsAmount, string text, SimpleCallback onCurrencyHittedTarget = null)
        {
            SpawnCurrency(key.GetHashCode(), rectTransform, targetTransform, elementsAmount, text, onCurrencyHittedTarget);
        }

        public static void SpawnCurrency(int hash, RectTransform rectTransform, RectTransform targetTransform, int elementsAmount, string text, SimpleCallback onCurrencyHittedTarget = null)
        {
            if (!floatingCloudLink.ContainsKey(hash))
            {
                Debug.LogError($"Cloud with hash {hash} isn't registered!");

                return;
            }

            Animation animation = new Animation(floatingCloudLink[hash], rectTransform, targetTransform, elementsAmount, onCurrencyHittedTarget);
            animation.PlayAnimation();

            activeClouds.Add(animation);
        }

        public static void OnAnimationFinished(Animation animation)
        {
            activeClouds.Remove(animation);
        }

        [System.Serializable]
        public class Data
        {
            [SerializeField] string name;
            public string Name => name;

            [SerializeField] GameObject prefab;
            public GameObject Prefab => prefab;

            [SerializeField] AudioClip appearAudioClip;
            public AudioClip AppearAudioClip => appearAudioClip;

            [SerializeField] AudioClip collectAudioClip;
            public AudioClip CollectAudioClip => collectAudioClip;

            [Space]
            [SerializeField] float cloudRadius;
            public float CloudRadius => cloudRadius;

            private readonly List<GameObject> pooledObjects = new List<GameObject>();
            private Transform container;

            public Data(FloatingCloudSettings settings)
            {
                name = settings.Name;
                prefab = settings.Prefab;

                cloudRadius = settings.CloudRadius;

                appearAudioClip = settings.AppearAudioClip;
                collectAudioClip = settings.CollectAudioClip;
            }

            public void Init()
            {
                if (prefab == null)
                {
                    Debug.LogError($"Floating Cloud ({name}) initialization failed. Prefab is missing.");
                    return;
                }

                GameObject containerObject = new GameObject($"FloatingCloud_{name}_Pool");
                container = containerObject.transform;
                container.hideFlags = HideFlags.HideInHierarchy;
            }

            public void Destroy()
            {
                for (int i = 0; i < pooledObjects.Count; i++)
                {
                    GameObject pooledObject = pooledObjects[i];
                    if (pooledObject == null)
                    {
                        continue;
                    }

#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        Object.DestroyImmediate(pooledObject);
                    }
                    else
#endif
                    {
                        Object.Destroy(pooledObject);
                    }
                }

                pooledObjects.Clear();

                if (container != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        Object.DestroyImmediate(container.gameObject);
                    }
                    else
#endif
                    {
                        Object.Destroy(container.gameObject);
                    }
                }

                container = null;
            }

            public GameObject GetInstance()
            {
                for (int i = 0; i < pooledObjects.Count; i++)
                {
                    GameObject pooledObject = pooledObjects[i];
                    if (pooledObject == null)
                    {
                        continue;
                    }

                    if (!pooledObject.activeSelf)
                    {
                        return pooledObject;
                    }
                }

                if (prefab == null)
                {
                    return null;
                }

                Transform targetParent = container != null ? container : null;
                GameObject instance = Object.Instantiate(prefab, targetParent);
                instance.SetActive(false);
                pooledObjects.Add(instance);

                return instance;
            }

            public void Release(GameObject instance)
            {
                if (instance == null)
                {
                    return;
                }

                if (container != null)
                {
                    instance.transform.SetParent(container, false);
                }

                instance.SetActive(false);
            }
        }

        public class Animation
        {
            private Data floatingCloudData;
            private RectTransform rectTransform;
            private RectTransform targetTransform;
            private int elementsAmount;
            private List<RectTransform> elementsList = new List<RectTransform>();
            private SimpleCallback onCurrencyHittedTarget;

            private Transform fakeTargetTransform;

            private TweenCaseCollection tweenCaseCollection;

            public Animation(Data floatingCloudData, RectTransform rectTransform, RectTransform targetTransform, int elementsAmount, SimpleCallback onCurrencyHittedTarget)
            {
                this.floatingCloudData = floatingCloudData;
                this.rectTransform = rectTransform;
                this.targetTransform = targetTransform;
                this.elementsAmount = elementsAmount;
                this.elementsList = new List<RectTransform>();
                this.onCurrencyHittedTarget = onCurrencyHittedTarget;

                GameObject fakeTargetObject = new GameObject("Fake Target");
                fakeTargetTransform = fakeTargetObject.transform;
                fakeTargetTransform.SetParent(targetTransform.parent);
                fakeTargetTransform.position = targetTransform.position;
                fakeTargetTransform.localScale = targetTransform.localScale;
                fakeTargetTransform.localRotation = targetTransform.localRotation;
            }

            public void PlayAnimation()
            {
                RectTransform targetRectTransform = targetTransform;

                tweenCaseCollection = Tween.BeginTweenCaseCollection();

                // Play appear sound
#if MODULE_AUDIO
                if (floatingCloudData.AppearAudioClip != null)
                    AudioController.PlaySound(floatingCloudData.AppearAudioClip);
#endif

                float cloudRadius = floatingCloudData.CloudRadius;
                Vector3 centerPoint = rectTransform.position;

                int finishedElementsAmount = 0;

                float defaultPitch = 0.9f;
                bool currencyHittedTarget = false;
                for (int i = 0; i < elementsAmount; i++)
                {
                    TweenCase currencyTweenCase = null;
                    GameObject elementObject = floatingCloudData.GetInstance();
                    if (elementObject == null)
                    {
                        continue;
                    }

                    RectTransform elementRectTransform = (RectTransform)elementObject.transform;

                    elementRectTransform.SetParent(fakeTargetTransform);

                    elementRectTransform.position = centerPoint;
                    elementRectTransform.localRotation = Quaternion.identity;
                    elementRectTransform.localScale = Vector3.one;

                    elementsList.Add(elementRectTransform);

                    Image elementImage = elementObject.GetComponent<Image>();
                    elementImage.color = Color.white.SetAlpha(0);

                    float moveTime = Random.Range(0.6f, 0.8f);

                    elementImage.DOFade(1, 0.2f, unscaledTime: true);
                    elementRectTransform.DOAnchoredPosition(elementRectTransform.anchoredPosition + (Random.insideUnitCircle * cloudRadius), moveTime, unscaledTime: true).SetEasing(Ease.Type.CubicOut).OnComplete(delegate
                    {
                        tweenCaseCollection.AddTween(Tween.DelayedCall(0.1f, delegate
                        {
                            tweenCaseCollection.AddTween(elementRectTransform.DOScale(0.3f, 0.5f, unscaledTime: true).SetEasing(Ease.Type.ExpoIn));
                            tweenCaseCollection.AddTween(elementRectTransform.DOLocalMove(Vector3.zero, 0.5f, unscaledTime: true).SetEasing(Ease.Type.SineIn).OnComplete(delegate
                            {
                                if (!currencyHittedTarget)
                                {
                                    if (onCurrencyHittedTarget != null)
                                        onCurrencyHittedTarget.Invoke();

                                    currencyHittedTarget = true;
                                }

                                bool punchTarget = true;
                                if (currencyTweenCase != null)
                                {
                                    if (currencyTweenCase.State < 0.8f)
                                    {
                                        punchTarget = false;
                                    }
                                    else
                                    {
                                        currencyTweenCase.Kill();
                                    }
                                }

                                if (punchTarget)
                                {
                                    // Play collect sound
                                    if (floatingCloudData.CollectAudioClip != null)
                                    {
#if MODULE_AUDIO
                                        AudioController.PlaySound(floatingCloudData.CollectAudioClip, pitch: defaultPitch);
#endif
                                    }

                                    defaultPitch += 0.01f;

                                    currencyTweenCase = targetRectTransform.DOScale(1.2f, 0.15f, unscaledTime: true).OnComplete(delegate
                                    {
                                        currencyTweenCase = targetRectTransform.DOScale(1.0f, 0.1f, unscaledTime: true);

                                        tweenCaseCollection.AddTween(currencyTweenCase);
                                    });

                                    tweenCaseCollection.AddTween(currencyTweenCase);
                                }

                                floatingCloudData.Release(elementObject);

                                finishedElementsAmount++;
                                if (finishedElementsAmount >= elementsAmount)
                                {
                                    FloatingCloud.OnAnimationFinished(this);

                                    GameObject.Destroy(fakeTargetTransform.gameObject);
                                }
                            }));
                        }, unscaledTime: true));
                    });
                }

                Tween.EndTweenCaseCollection();
            }

            public void Clear()
            {
                tweenCaseCollection.Kill();

                if(fakeTargetTransform != null)
                    GameObject.Destroy(fakeTargetTransform.gameObject);
            }
        }
    }
}
