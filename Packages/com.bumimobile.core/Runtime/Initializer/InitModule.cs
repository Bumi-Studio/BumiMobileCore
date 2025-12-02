using System.Collections;
using UnityEngine;

namespace BumiMobile
{
    public abstract class InitModule : ScriptableObject
    {
        public abstract string ModuleName { get; }
        public abstract void CreateComponent();
        public virtual bool IsAsync => false;
        public virtual IEnumerator InitializeCoroutine(Initializer initializer)
        {
            // default: jalanin sync path
            CreateComponent();
            yield break;
        }
    }
}
