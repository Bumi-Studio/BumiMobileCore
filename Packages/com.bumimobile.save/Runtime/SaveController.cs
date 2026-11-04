using System;
using System.Collections;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;

namespace BumiMobile
{
    [StaticUnload]
    public static class SaveController
    {
        public enum SaveLoadPhase { LocalLoaded, CloudApplied, CloudSkipped }
        public static event System.Action<SaveLoadPhase> OnSavePhase;
        private static void InvokePhase(SaveLoadPhase phase)
        {
            OnSaveLoaded?.Invoke();         // backward-compat (dipanggil saat Local & Cloud)
            OnSavePhase?.Invoke(phase);     // fase spesifik
        }
        private static string saveFileName = "save";

        private static GlobalSave globalSave;
        private static readonly object localSaveGate = new object();
        private static long localSaveRevision;

        public static GlobalSave GlobalSave
        {
            get => globalSave;
            set
            {
                lock (localSaveGate)
                {
                    Interlocked.Increment(ref localSaveRevision);
                    globalSave = value;
                }
            }
        }

        private static bool isSaveLoaded;
        public static bool IsSaveLoaded => isSaveLoaded;

        private static volatile bool isSaveRequired;

        public static float GameTime => globalSave.GameTime;

        public static DateTime LastExitTime => globalSave.LastExitTime;

        public static event SimpleCallback OnSaveLoaded;

        public static void Init(float autoSaveDelay, GlobalSave initialGlobalSave, bool clearSave = false, float overrideTime = -1f, int saveSlotIndex = 0)
        {
            isSaveLoaded = false;
            saveFileName = saveSlotIndex > 0 ? $"save_{saveSlotIndex}" : "save";
            GlobalSave = initialGlobalSave ?? new GlobalSave();
            Serializer.Init();

            GameObject saveCallbackReciever = new GameObject("[SAVE CALLBACK RECIEVER]");
            saveCallbackReciever.hideFlags = HideFlags.HideInHierarchy;

            GameObject.DontDestroyOnLoad(saveCallbackReciever);

            UnityCallbackReciever unityCallbackReciever = saveCallbackReciever.AddComponent<UnityCallbackReciever>();

            if (clearSave)
            {
                InitClear(overrideTime != -1f ? overrideTime : Time.time);
            }
            else
            {
                Load(overrideTime != -1f ? overrideTime : Time.time);
            }

            if (autoSaveDelay > 0)
            {
                // Enable auto-save coroutine
                unityCallbackReciever.StartCoroutine(AutoSaveCoroutine(autoSaveDelay));
            }
        }

        public static void UpdateTime(float time)
        {
            globalSave.Time = time;
        }

        public static T GetSaveObject<T>(int hash) where T : ISaveObject, new()
        {
            if (!isSaveLoaded)
            {
                Debug.LogError("Save controller has not been initialized");
                return default;
            }

            return globalSave.GetSaveObject<T>(hash);
        }

        public static T GetSaveObject<T>(string uniqueName) where T : ISaveObject, new()
        {
            return GetSaveObject<T>(uniqueName.GetHashCode());
        }

        private static void InitClear(float time)
        {
            GlobalSave = new GlobalSave();
            globalSave.Init(time);

            Debug.Log("[Save Controller]: Created clear save!");

            isSaveLoaded = true;
        }

        private static void Load(float time)
        {
            if (isSaveLoaded)
                return;

            // Try to read and deserialize file or create new one
            globalSave = BaseSaveWrapper.ActiveWrapper.Load(saveFileName);

            globalSave.Init(time);

            Debug.Log("[Save Controller]: Save is loaded!");

            isSaveLoaded = true;

            OnSaveLoaded?.Invoke();
        }

        // All public save entry points must be called on the Unity thread. Only detached
        // snapshots go to workers; a queued older snapshot cannot overwrite a newer request.
        public static void Save(bool forceSave = false, bool useThreads = true, bool saveCloudImmediately = false, bool saveCloud = true)
        {
            if ((!forceSave && !isSaveRequired) || !isSaveLoaded || globalSave == null) return;
            BaseSaveWrapper wrapper = BaseSaveWrapper.ActiveWrapper;
            if (!TryCapture(globalSave, true, out GlobalSave snapshot, out long revision, out string error))
            {
                Debug.LogError("[Save Controller] Local save failed: " + error);
                return;
            }

            string fileName = saveFileName;
            SynchronizationContext context = SynchronizationContext.Current;
            bool runInBackground = useThreads && !saveCloudImmediately && wrapper.UseThreads()
                && (!saveCloud || context != null);

            void PublishCloud()
            {
                if (!saveCloud || revision != Interlocked.Read(ref localSaveRevision)
                    || !ReferenceEquals(wrapper, BaseSaveWrapper.ActiveWrapper)) return;
                if (saveCloudImmediately) wrapper.SaveCloudNow(snapshot);
                else wrapper.SaveCloud(snapshot);
            }

            void WriteSnapshot()
            {
                if (!TryWrite(snapshot, wrapper, fileName, revision, false, out string writeError))
                {
                    if (writeError != null) Debug.LogError("[Save Controller] Local save failed: " + writeError);
                    return;
                }
                if (runInBackground && saveCloud) context.Post(_ => PublishCloud(), null);
                else PublishCloud();
            }

            if (runInBackground) new Thread(WriteSnapshot) { IsBackground = true }.Start();
            else WriteSnapshot();
        }

        /// <summary>
        /// Synchronously saves ALL modules together. True means the local file was replaced
        /// successfully. Never uploads to cloud. Callers must roll back staged mutations on
        /// failure before yielding, publishing events, or allowing another save.
        /// </summary>
        public static bool TrySaveLocal(out string error)
        {
            return TrySaveLocalSnapshot(out _, out _, out error);
        }

        /// <summary>
        /// Synchronously saves ALL modules locally and queues the same snapshot
        /// for cloud upload. True means the local file was replaced successfully;
        /// cloud upload remains best effort and may complete later.
        /// </summary>
        public static bool TrySaveLocalAndQueueCloud(out string error)
        {
            if (!TrySaveLocalSnapshot(out GlobalSave snapshot, out BaseSaveWrapper wrapper, out error))
                return false;

            try
            {
                wrapper.SaveCloud(snapshot);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Save Controller] Cloud upload could not be queued after local save: " + exception.Message);
            }

            return true;
        }

        private static bool TrySaveLocalSnapshot(out GlobalSave snapshot, out BaseSaveWrapper wrapper, out string error)
        {
            snapshot = null;
            wrapper = BaseSaveWrapper.ActiveWrapper;
            error = null;
            if (!isSaveLoaded || globalSave == null)
            {
                error = "Save controller has not loaded player data.";
                return false;
            }
            if (!wrapper.SupportsDurableLocalSave)
            {
                error = "This platform cannot verify durable local saves.";
                return false;
            }
            if (!TryCapture(globalSave, true, out snapshot, out long revision, out error)) return false;
            bool saved = TryWrite(snapshot, wrapper, saveFileName, revision, true, out error);
            if (!saved && error == null) error = "The save was superseded by another save or profile.";
            return saved;
        }

        private static bool TryCapture(GlobalSave source, bool updateTime, out GlobalSave snapshot,
            out long revision, out string error)
        {
            revision = Interlocked.Increment(ref localSaveRevision);
            snapshot = null;
            error = null;
            isSaveRequired = false;
            try
            {
                snapshot = source.CreateSnapshot(updateTime);
                return true;
            }
            catch (Exception exception)
            {
                isSaveRequired = true;
                error = exception.Message;
                return false;
            }
        }

        private static bool TryWrite(GlobalSave snapshot, BaseSaveWrapper wrapper, string fileName,
            long revision, bool requireDurability, out string error)
        {
            error = null;
            lock (localSaveGate)
            {
                if (revision != Interlocked.Read(ref localSaveRevision)) return false;
                try
                {
                    if (requireDurability)
                    {
                        if (wrapper.TrySaveLocal(snapshot, fileName, out error)) return true;
                        if (error == null) error = "Local save failed.";
                    }
                    else
                    {
                        wrapper.SaveLocal(snapshot, fileName);
                        return true;
                    }
                }
                catch (Exception exception)
                {
                    error = exception.Message;
                }
                if (revision == Interlocked.Read(ref localSaveRevision)) isSaveRequired = true;
                return false;
            }
        }

        [UnityEngine.Scripting.Preserve]
        public static async Task<bool> SaveAndWaitForCloudAsync(bool forceSave = false)
        {
            // Always establish a checkpoint, including when an earlier autosave is pending.
            if (!TrySaveLocalSnapshot(out GlobalSave snapshot, out BaseSaveWrapper wrapper, out _)) return false;
            return await wrapper.SaveCloudNowAsync(snapshot);
        }

        public static void SaveCustom(GlobalSave customSave)
        {
            if (customSave == null) return;
            if (!TryCapture(customSave, false, out GlobalSave snapshot, out long revision, out string error))
                throw new InvalidOperationException(error);
            BaseSaveWrapper wrapper = BaseSaveWrapper.ActiveWrapper;
            if (!TryWrite(snapshot, wrapper, saveFileName, revision, false, out error))
                throw new InvalidOperationException(error ?? "Custom save was superseded.");
            wrapper.SaveCloud(snapshot);
        }

        public static void MarkAsSaveIsRequired()
        {
            isSaveRequired = true;
        }

        private static IEnumerator AutoSaveCoroutine(float saveDelay)
        {
            WaitForSeconds waitForSeconds = new WaitForSeconds(saveDelay);

            while (true)
            {
                yield return waitForSeconds;

                Save();
            }
        }

        public static void PresetsSave(string fullFileName)
        {
            BaseSaveWrapper.ActiveWrapper.Save(globalSave.CreateSnapshot(false), fullFileName);
        }

        public static void Info()
        {
            globalSave.Info();
        }

        public static void DeleteSaveFile()
        {
            lock (localSaveGate)
            {
                Interlocked.Increment(ref localSaveRevision);
                BaseSaveWrapper.ActiveWrapper.Delete(saveFileName);
            }
        }

        public static GlobalSave GetGlobalSave()
        {
            GlobalSave tempGlobalSave = BaseSaveWrapper.ActiveWrapper.Load(saveFileName);

            tempGlobalSave.Init(Time.time);

            return tempGlobalSave;
        }

        private static void UnloadStatic()
        {
            GlobalSave = null;

            isSaveLoaded = false;
            isSaveRequired = false;

            OnSaveLoaded = null;
        }
        public static void BeginCloudLoadAndReplace()
        {
            if (!BaseSaveWrapper.Active.SupportsCloud)
            {
                OnSavePhase?.Invoke(SaveLoadPhase.CloudSkipped);
                return;
            }

            GlobalSave localSave = globalSave;
            BaseSaveWrapper.Active.BeginCloudLoad(cloud =>
            {
                if (cloud == null)
                {
                    OnSavePhase?.Invoke(SaveLoadPhase.CloudSkipped);
                    return;
                }

                cloud.Init(Time.time);

                if (ShouldUseCloudSave(localSave, cloud))
                {
                    GlobalSave = cloud;
                    isSaveLoaded = true;

                    Debug.Log("[Save Controller] Cloud applied.");
                    InvokePhase(SaveLoadPhase.CloudApplied);
                    return;
                }

                if (localSave != null)
                {
                    Debug.Log("[Save Controller] Local save is newer than cloud. Keeping local data and syncing it up.");
                    BaseSaveWrapper.ActiveWrapper.SaveCloudNow(localSave.CreateSnapshot(false));
                }

                OnSavePhase?.Invoke(SaveLoadPhase.CloudSkipped);
            });
        }

        private static bool ShouldUseCloudSave(GlobalSave localSave, GlobalSave cloudSave)
        {
            if (cloudSave == null)
            {
                return false;
            }

            if (localSave == null)
            {
                return true;
            }

            DateTime localExitTime = localSave.LastExitTime;
            DateTime cloudExitTime = cloudSave.LastExitTime;

            if (cloudExitTime == DateTime.MinValue)
            {
                return false;
            }

            if (localExitTime == DateTime.MinValue)
            {
                return true;
            }

            return cloudExitTime > localExitTime;
        }

        private class UnityCallbackReciever : MonoBehaviour
        {
            private void OnDestroy()
            {
#if UNITY_EDITOR
                SaveController.Save(true, useThreads: false);
#endif
            }

            private void OnApplicationFocus(bool focus)
            {
#if !UNITY_EDITOR
                if(!focus) SaveController.Save(useThreads: false);
#endif
            }
        }
    }
}
