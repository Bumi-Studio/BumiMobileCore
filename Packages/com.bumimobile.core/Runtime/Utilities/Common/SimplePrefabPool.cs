using System.Collections.Generic;
using UnityEngine;

namespace BumiMobile
{
    public sealed class SimplePrefabPool
    {
        private readonly GameObject prefab;
        private readonly List<GameObject> instances = new List<GameObject>();
        private readonly Transform container;
        private readonly bool isValid;

        public SimplePrefabPool(GameObject prefab, string name, Transform parent = null)
        {
            if (prefab == null)
            {
                Debug.LogError("[SimplePrefabPool]: Prefab reference is null.");
                isValid = false;
                return;
            }

            this.prefab = prefab;

            string containerName = string.IsNullOrEmpty(name) ? "[SimplePrefabPool]" : name;
            GameObject containerObject = new GameObject(containerName);
            container = containerObject.transform;
            container.SetParent(parent, false);
            container.hideFlags = HideFlags.HideInHierarchy;
            Object.DontDestroyOnLoad(container.gameObject);

            isValid = true;
        }

        public Transform Container => container;

        public GameObject Get()
        {
            if (!isValid)
            {
                return null;
            }

            for (int i = 0; i < instances.Count; i++)
            {
                GameObject instance = instances[i];
                if (instance == null)
                {
                    continue;
                }

                if (!instance.activeSelf)
                {
                    return instance;
                }
            }

            GameObject newInstance = Object.Instantiate(prefab, container);
            newInstance.SetActive(false);
            instances.Add(newInstance);

            return newInstance;
        }

        public void Release(GameObject instance, bool resetParent)
        {
            if (!isValid || instance == null)
            {
                return;
            }

            if (resetParent)
            {
                instance.transform.SetParent(container, false);
            }

            instance.SetActive(false);
        }

        public void ReleaseAll(bool resetParent)
        {
            if (!isValid)
            {
                return;
            }

            for (int i = 0; i < instances.Count; i++)
            {
                GameObject instance = instances[i];
                if (instance == null)
                {
                    continue;
                }

                Release(instance, resetParent);
            }
        }

        public void Dispose()
        {
            if (!isValid)
            {
                return;
            }

            for (int i = 0; i < instances.Count; i++)
            {
                GameObject instance = instances[i];
                if (instance == null)
                {
                    continue;
                }

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    Object.DestroyImmediate(instance);
                }
                else
#endif
                {
                    Object.Destroy(instance);
                }
            }

            instances.Clear();

            if (container == null)
            {
                return;
            }

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
    }
}
