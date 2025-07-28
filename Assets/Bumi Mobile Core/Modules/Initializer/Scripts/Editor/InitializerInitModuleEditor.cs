using UnityEngine;
using UnityEditor;

namespace BumiMobile
{
    [CustomEditor(typeof(InitializerInitModule))]
    public class InitializerInitModuleEditor : InitModuleEditor
    {
        public override void OnCreated()
        {
            GameObject canvasPrefab = EditorUtils.GetAsset<GameObject>("Core System Messages Canvas");
            if (canvasPrefab != null)
            {
                serializedObject.Update();
                serializedObject.FindProperty("systemMessagesPrefab").objectReferenceValue = canvasPrefab;
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
