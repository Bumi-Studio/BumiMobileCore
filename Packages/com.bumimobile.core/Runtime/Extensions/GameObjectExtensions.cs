using UnityEngine;

namespace BumiMobile
{
    public static class GameObjectExtensions
    {
        public static T GetOrSetComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();

            if (component != null)
                return component;

            return gameObject.AddComponent<T>();
        }
    }
}
