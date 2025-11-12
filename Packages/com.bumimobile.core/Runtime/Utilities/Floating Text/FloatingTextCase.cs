using UnityEngine;

namespace BumiMobile
{
    [System.Serializable]
    public class FloatingTextCase
    {
        [SerializeField] string name;
        public string Name => name;

        [SerializeField] FloatingTextBaseBehavior floatingTextBehavior;
        public FloatingTextBaseBehavior FloatingTextBehavior => floatingTextBehavior;

        private SimplePrefabPool floatingTextPool;

        public void Init()
        {
            if (floatingTextPool != null)
            {
                return;
            }

            if (floatingTextBehavior == null)
            {
                Debug.LogError($"[Floating Text]: FloatingTextCase '{name}' is missing behavior reference.");
                return;
            }

            floatingTextPool = new SimplePrefabPool(floatingTextBehavior.gameObject, $"FloatingText_{name}_Pool");
        }

        public GameObject GetInstance()
        {
            return floatingTextPool?.Get();
        }

        public void Release(GameObject instance)
        {
            floatingTextPool?.Release(instance, true);
        }

        public void ReleaseAll()
        {
            floatingTextPool?.ReleaseAll(true);
        }

        public void Dispose()
        {
            floatingTextPool?.Dispose();
            floatingTextPool = null;
        }
    }
}