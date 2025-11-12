using UnityEditor;

namespace BumiMobile
{
    [CustomEditor(typeof(CoreSettings))]
    public class CoreSettingsEditor : Editor
    {
        private CoreSettings coreSettings;

        private void OnEnable()
        {
            coreSettings = (CoreSettings)target;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            base.OnInspectorGUI();

            if (EditorGUI.EndChangeCheck() && coreSettings != null)
            {
                CoreEditor.ApplySettings(coreSettings);
            }
        }

        [MenuItem("Window/Bumi Mobile Core/Core Settings", priority = 50)]
        private static void SelectSettings()
        {
            CoreSettings settings = EditorUtils.GetAsset<CoreSettings>();
            if (settings == null)
                return;

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }
    }
}
