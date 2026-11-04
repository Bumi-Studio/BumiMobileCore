using System;
using UnityEngine;
using System.Collections.Generic;

namespace BumiMobile
{
    [Serializable]
    public class GlobalSave
    {
        [SerializeField] SavedDataContainer[] saveObjects;
        private List<SavedDataContainer> saveObjectsList;

        [SerializeField] float gameTime;
        public float GameTime => gameTime + (Time - lastFlushTime);

        [SerializeField] DateTime lastExitTime;
        public DateTime LastExitTime => lastExitTime;

        private float lastFlushTime = 0;

        public float Time { get; set; }

        public void Init(float time)
        {
            if (saveObjects == null)
            {
                saveObjectsList = new List<SavedDataContainer>();
            }
            else
            {
                saveObjectsList = new List<SavedDataContainer>(saveObjects);
            }

            for (int i = 0; i < saveObjectsList.Count; i++)
            {
                saveObjectsList[i].Restored = false;
            }

            Time = time;
            lastFlushTime = Time;
        }

        public void Flush(bool updateLastExitTime)
        {
            saveObjects = saveObjectsList.ToArray();

            for (int i = 0; i < saveObjectsList.Count; i++)
            {
                SavedDataContainer saveObject = saveObjectsList[i];

                saveObject.Flush();
            }

            gameTime += Time - lastFlushTime;

            lastFlushTime = Time;

            if(updateLastExitTime) lastExitTime = DateTime.Now;
        }

        /// <summary>Call on the Unity thread. The returned save shares no live modules.</summary>
        public GlobalSave CreateSnapshot(bool updateLastExitTime)
        {
            Flush(updateLastExitTime);
            GlobalSave snapshot = new GlobalSave();
            snapshot.saveObjects = new SavedDataContainer[saveObjects.Length];
            for (int i = 0; i < saveObjects.Length; i++)
            {
                snapshot.saveObjects[i] = saveObjects[i].CreateSnapshot();
            }
            snapshot.saveObjectsList = new List<SavedDataContainer>(snapshot.saveObjects);
            snapshot.gameTime = gameTime;
            snapshot.lastExitTime = lastExitTime;
            snapshot.Time = Time;
            snapshot.lastFlushTime = lastFlushTime;
            return snapshot;
        }

        public T GetSaveObject<T>(int hash) where T : ISaveObject, new()
        {
            SavedDataContainer container = saveObjectsList.Find((container) => container.Hash == hash);

            if (container == null)
            {
                container = new SavedDataContainer(hash, new T());

                saveObjectsList.Add(container);

            }
            else
            {
                if (!container.Restored) container.Restore<T>();
            }
            return (T)container.SaveObject;
        }

        public T GetSaveObject<T>(string uniqueName) where T : ISaveObject, new()
        {
            return GetSaveObject<T>(uniqueName.GetHashCode());
        }

        public void Info()
        {
            foreach (var container in saveObjectsList)
            {
                Debug.Log("Hash: " + container.Hash);
                Debug.Log("Save Object: " + container.SaveObject);
            }
        }
    }
}
