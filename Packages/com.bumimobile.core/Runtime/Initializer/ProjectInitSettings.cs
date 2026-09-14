#pragma warning disable 0649

using UnityEngine;

namespace BumiMobile
{
    public class ProjectInitSettings : ScriptableObject
    {
        [SerializeField] InitModule[] modules;
        public InitModule[] Modules => modules;

        public void Init(Initializer initializer)
        {
            initializer?.ConfigureModules(modules);
        }

        public T GetModule<T>() where T : InitModule
        {
            foreach (var module in modules)
            {
                if (module is T typedModule)
                {
                    return typedModule;
                }
            }

            return null;
        }
    }
}
