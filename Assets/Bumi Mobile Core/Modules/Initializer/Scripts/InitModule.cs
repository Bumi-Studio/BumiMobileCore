using UnityEngine;

namespace BumiMobile
{
    public abstract class InitModule : ScriptableObject
    {
        public abstract string ModuleName { get; }

        public abstract void CreateComponent();
    }
}