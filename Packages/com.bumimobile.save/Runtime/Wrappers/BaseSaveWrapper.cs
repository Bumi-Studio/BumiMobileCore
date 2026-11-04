using System;

using System.Threading.Tasks;

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
        public virtual bool SupportsDurableLocalSave => false;

        public virtual bool TrySaveLocal(GlobalSave snapshot, string fileName, out string error)
        {
            error = null;
            if (!SupportsDurableLocalSave)
            {
                error = "This save wrapper cannot verify durable local writes.";
                return false;
            }
            try
            {
                SaveLocal(snapshot, fileName);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }
        public abstract GlobalSave Load(string fileName);
        public abstract void Save(GlobalSave globalSave, string fileName);
        public abstract void Delete(string fileName);

        public virtual bool UseThreads() { return false; }

        public virtual void SaveLocal(GlobalSave globalSave, string fileName)
        {
            Save(globalSave, fileName);
        }

        public virtual void BeginCloudLoad(Action<GlobalSave> onLoaded)
        {
            onLoaded?.Invoke(null);
        }

        /// <summary>True means a successful read, including confirmed absence. Null alone is not proof.</summary>
        public virtual void BeginCloudLoadChecked(Action<GlobalSave, bool> onLoaded)
        {
            BeginCloudLoad(save => onLoaded?.Invoke(save, save != null));
        }

        public virtual void SaveCloud(GlobalSave globalSave)
        {
            /* no-op */
        }

        public virtual void SaveCloudNow(GlobalSave globalSave)
        {
            SaveCloud(globalSave);
        }

        public virtual Task<bool> SaveCloudNowAsync(GlobalSave globalSave)
        {
            SaveCloudNow(globalSave);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Deletes every cloud save this device can reach for the current account (for account
        /// deletion). True only when the documents are confirmed gone. Wrappers that cannot reach
        /// the cloud return false, so callers never treat "not attempted" as "deleted".
        /// </summary>
        public virtual Task<bool> DeleteCloudSavesAsync()
        {
            return Task.FromResult(false);
        }
    }
}
