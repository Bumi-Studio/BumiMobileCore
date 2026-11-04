using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace BumiMobile
{
    public class WebGLSaveWrapper : BaseSaveWrapper
    {
        public bool LastLoadFailed { get; private set; }

        public override GlobalSave Load(string fileName)
        {
            LastLoadFailed = false;
#if UNITY_WEBGL && !UNITY_EDITOR
            string jsonObject = load(fileName);
            if(!string.IsNullOrEmpty(jsonObject))
            {
                try
                {
                    GlobalSave deserializedObject = JsonUtility.FromJson<GlobalSave>(jsonObject);
                    if (deserializedObject == null) throw new System.InvalidDataException("The WebGL save JSON produced no save object.");
                    return deserializedObject;
                }
                catch (System.Exception ex)
                {
                    LastLoadFailed = true;
                    Debug.LogError("[Save] WebGL save data is corrupt and was not loaded: " + ex.Message);
                }
            }
#endif

            if (LastLoadFailed)
                Debug.LogWarning("[Save] Starting a clean WebGL session; cloud recovery must resolve ownership before upload.");
            return new GlobalSave();
        }

        public override void Save(GlobalSave globalSave, string fileName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string jsonObject = JsonUtility.ToJson(globalSave);

            save(fileName, jsonObject);
#endif
        }

        public override void Delete(string fileName)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            deleteItem(fileName);
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern string load(string keyName);

        [DllImport("__Internal")]
        private static extern void save(string keyName, string data);

        [DllImport("__Internal")]
        private static extern void deleteItem(string keyName);
#endif
    }
}
