using System;

namespace BumiMobile
{
    // Stub save wrapper used when Firestore is temporarily disabled
    public sealed class FirebaseSaveWrapperDisabled : BaseSaveWrapper
    {
        private readonly DefaultSaveWrapper _local = new DefaultSaveWrapper();

        public override bool SupportsCloud => false;

        public override GlobalSave Load(string fileName) => _local.Load(fileName);
        public override void Save(GlobalSave globalSave, string fileName) => _local.Save(globalSave, fileName);
        public override void Delete(string fileName) => _local.Delete(fileName);

        public override bool UseThreads() => _local.UseThreads();

        public override void BeginCloudLoad(Action<GlobalSave> onLoaded)
        {
            onLoaded?.Invoke(null); // no cloud when disabled
        }

        public override void SaveCloud(GlobalSave globalSave)
        {
            // no-op while cloud is disabled
        }
    }
}

