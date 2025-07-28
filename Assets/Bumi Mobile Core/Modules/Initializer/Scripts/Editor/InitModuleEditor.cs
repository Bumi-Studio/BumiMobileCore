using UnityEditor;

namespace BumiMobile
{
    public abstract class InitModuleEditor : CustomInspector
    {
        public virtual void OnCreated() { }
        public virtual void OnRemoved() { }

        public virtual void Buttons() { }

        public virtual void PrepareMenuItems(ref GenericMenu genericMenu) { }
    }
}