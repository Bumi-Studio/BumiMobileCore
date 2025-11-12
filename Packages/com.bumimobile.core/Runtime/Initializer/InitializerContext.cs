using UnityEngine;

namespace BumiMobile
{
    public static class InitializerContext
    {
        public static GameObject GameObject { get; private set; }
        public static Transform Transform { get; private set; }

        public static void Set(GameObject gameObject, Transform transform)
        {
            GameObject = gameObject;
            Transform = transform;
        }
    }
}
