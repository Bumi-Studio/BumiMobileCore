using System.Collections;
using UnityEngine;

namespace BumiMobile
{
    [RegisterModule("Save Controller", core: true, order: 900)]
    public class SaveInitModule : InitModule
    {
        const string SKIP_CLOUD_KEY = "__reset_skip_cloud__";
        public override string ModuleName => "Save Controller";

        [SerializeField] float autoSaveDelay = 0;
        [SerializeField] bool clearSave = false;
        [SerializeField] bool cleanSaveStart = false;
        [SerializeField] bool devCloudSave = false;
        [SerializeField] float cloudTimeout = 6f;
        [SerializeField] int saveSlotIndex = 0;
        public override bool IsAsync => true;

        public override void CreateComponent()
        {
            ApplyCloudSaveEnvironment();
            SaveController.Init(autoSaveDelay, new GlobalSave(), clearSave, saveSlotIndex: saveSlotIndex);
        }
        public override IEnumerator InitializeCoroutine(Initializer initializer)
        {
            ApplyCloudSaveEnvironment();
            SaveController.Init(autoSaveDelay, new GlobalSave(), clearSave, saveSlotIndex: saveSlotIndex);

            if (PlayerPrefs.GetInt(SKIP_CLOUD_KEY, 0) == 1)
            {
                PlayerPrefs.DeleteKey(SKIP_CLOUD_KEY);
                yield break; // lewati cloud load sekali ini
            }

            // Debug.Log("[Save Controller] Authenticated? " + PlayGamesPlatform.Instance.localUser.authenticated);

#if UNITY_ANDROID 
            bool done = false;
            void PhaseHandler(SaveController.SaveLoadPhase phase)
            {
                if (phase is SaveController.SaveLoadPhase.CloudApplied or SaveController.SaveLoadPhase.CloudSkipped)
                {
                    done = true;
                }
            }

            SaveController.OnSavePhase += PhaseHandler;
            SaveController.BeginCloudLoadAndReplace();

            float start = Time.realtimeSinceStartup;
            while (!done && (Time.realtimeSinceStartup - start) < cloudTimeout)
                yield return null;

            SaveController.OnSavePhase -= PhaseHandler;
            Debug.Log("[Save Controller] cloud loaded");
#else
            yield break;
#endif
        }

        void ApplyCloudSaveEnvironment()
        {
#if BUMI_SAVE_CLOUD_FIREBASE
            FirebaseSaveWrapper.UseDevCollection = devCloudSave;
#endif
        }

    }

}
