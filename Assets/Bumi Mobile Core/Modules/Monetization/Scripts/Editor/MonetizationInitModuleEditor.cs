using UnityEditor;

namespace BumiMobile
{
    [CustomEditor(typeof(MonetizationInitModule))]
    public class MonetizationInitModuleEditor : InitModuleEditor
    {
        public override void OnCreated()
        {
            MonetizationSettings monetizationSettings = EditorUtils.GetAsset<MonetizationSettings>();
            if (monetizationSettings == null)
            {
                monetizationSettings = MonetizationSettingsEditor.CreateAsset(false);
            }

            serializedObject.Update();
            serializedObject.FindProperty("settings").objectReferenceValue = monetizationSettings;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
