using UnityEditor;
using UnityEngine;

namespace BumiMobile
{
    [CustomEditor(typeof(EventSpriteLibrary))]
    public class EventSpriteLibraryEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawScriptField();
            EventSpriteLibraryEditorGUI.DrawLibraryInspector((EventSpriteLibrary)target, true);
        }

        private void DrawScriptField()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Script", MonoScript.FromScriptableObject((ScriptableObject)target), typeof(MonoScript), false);
            }
        }
    }
}
