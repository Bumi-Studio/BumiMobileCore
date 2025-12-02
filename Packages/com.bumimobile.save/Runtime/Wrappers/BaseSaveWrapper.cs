using System;

namespace BumiMobile
{
    public abstract class BaseSaveWrapper
    {
        private static BaseSaveWrapper _active;
        public static BaseSaveWrapper Active => _active ??= ActiveWrapper;
        public static BaseSaveWrapper ActiveWrapper =
#if UNITY_EDITOR
            new DefaultSaveWrapper();
#elif UNITY_WEBGL
            new WebGLSaveWrapper();
#elif UNITY_ANDROID && BUMI_SAVE_CLOUD_GPGS
            new AndroidSaveWrapper();
#elif UNITY_ANDROID && BUMI_SAVE_CLOUD_FIREBASE
            new FirebaseSaveWrapper();
#else
            new DefaultSaveWrapper();
#endif
        public virtual bool SupportsCloud => false;
        public abstract GlobalSave Load(string fileName);
        public abstract void Save(GlobalSave globalSave, string fileName);
        public abstract void Delete(string fileName);

        public virtual bool UseThreads() { return false; }
        public virtual void BeginCloudLoad(Action<GlobalSave> onLoaded)
        {
            onLoaded?.Invoke(null);
        }

        public virtual void SaveCloud(GlobalSave globalSave)
        {
            /* no-op */
        }
    }
}
