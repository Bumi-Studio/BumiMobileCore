#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BumiMobile
{
    /// <summary>
    /// Custom property drawer for SoundKey.
    /// Shows Group and Id as dropdowns populated live from the AudioLibrary asset.
    /// Dropdowns auto-update whenever AudioLibrary changes — no manual step needed.
    /// </summary>
    [CustomPropertyDrawer(typeof(SoundKey))]
    public class SoundKeyDrawer : PropertyDrawer
    {
        private const string LibraryResourcePath = "AudioLibrary";
        private const float Spacing = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Label row + Group row + Id row
            return EditorGUIUtility.singleLineHeight * 3 + Spacing * 2;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Draw foldout label
            float lineH = EditorGUIUtility.singleLineHeight;
            Rect labelRect = new Rect(position.x, position.y, position.width, lineH);
            EditorGUI.LabelField(labelRect, label, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            SerializedProperty groupProp = property.FindPropertyRelative("Group");
            SerializedProperty idProp    = property.FindPropertyRelative("Id");

            AudioLibrary library = Resources.Load<AudioLibrary>(LibraryResourcePath);

            Rect groupRect = new Rect(position.x, position.y + lineH + Spacing, position.width, lineH);
            Rect idRect    = new Rect(position.x, position.y + (lineH + Spacing) * 2, position.width, lineH);

            if (library == null)
            {
                // Fallback: plain text fields if library not found
                EditorGUI.PropertyField(groupRect, groupProp, new GUIContent("Group"));
                EditorGUI.PropertyField(idRect,    idProp,    new GUIContent("Id"));
                EditorGUI.HelpBox(idRect, $"AudioLibrary not found at Resources/{LibraryResourcePath}", MessageType.Warning);
            }
            else
            {
                DrawGroupDropdown(groupRect, groupProp, idProp, library);
                DrawIdDropdown(idRect, groupProp, idProp, library);
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        private void DrawGroupDropdown(Rect rect, SerializedProperty groupProp, SerializedProperty idProp, AudioLibrary library)
        {
            string[] groups = library.GetGroupNames().OrderBy(g => g).ToArray();

            if (groups.Length == 0)
            {
                EditorGUI.LabelField(rect, "Group", "(no groups in AudioLibrary)");
                return;
            }

            int currentIndex = System.Array.IndexOf(groups, groupProp.stringValue);
            if (currentIndex < 0) currentIndex = 0;

            int newIndex = EditorGUI.Popup(rect, "Group", currentIndex, groups);

            if (newIndex != currentIndex || groupProp.stringValue != groups[newIndex])
            {
                groupProp.stringValue = groups[newIndex];

                // Auto-reset Id to first available in new group
                string[] ids = library.GetIdsInGroup(groups[newIndex]).OrderBy(i => i).ToArray();
                idProp.stringValue = ids.Length > 0 ? ids[0] : string.Empty;
            }
        }

        private void DrawIdDropdown(Rect rect, SerializedProperty groupProp, SerializedProperty idProp, AudioLibrary library)
        {
            string[] ids = library.GetIdsInGroup(groupProp.stringValue).OrderBy(i => i).ToArray();

            if (ids.Length == 0)
            {
                EditorGUI.LabelField(rect, "Id", "(no ids in this group)");
                return;
            }

            int currentIndex = System.Array.IndexOf(ids, idProp.stringValue);
            if (currentIndex < 0) currentIndex = 0;

            int newIndex = EditorGUI.Popup(rect, "Id", currentIndex, ids);
            idProp.stringValue = ids[newIndex];
        }
    }
}
#endif
