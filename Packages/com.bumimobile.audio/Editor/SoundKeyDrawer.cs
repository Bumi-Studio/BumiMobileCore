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

        // Cache the AudioLibrary so we don't call Resources.Load on every OnGUI repaint.
        // Invalidated by CacheInvalidator AssetPostprocessor whenever assets change.
        private static AudioLibrary _cachedLibrary;
        private static bool _cacheInitialized;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Label row + Group row + Id row
            float height = EditorGUIUtility.singleLineHeight * 3 + Spacing * 2;

            // Reserve extra line for the warning HelpBox when library is missing
            if (!_cacheInitialized)
                RefreshCache();
            if (_cachedLibrary == null)
                height += EditorGUIUtility.singleLineHeight + Spacing;

            return height;
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

            if (!_cacheInitialized)
                RefreshCache();

            float yBase = position.y + lineH + Spacing;
            Rect groupRect = new Rect(position.x, yBase, position.width, lineH);
            Rect idRect    = new Rect(position.x, yBase + lineH + Spacing, position.width, lineH);

            if (_cachedLibrary == null)
            {
                // Fallback: plain text fields if library not found
                EditorGUI.PropertyField(groupRect, groupProp, new GUIContent("Group"));
                EditorGUI.PropertyField(idRect,    idProp,    new GUIContent("Id"));

                // HelpBox on its own line below Id (space reserved in GetPropertyHeight)
                Rect helpRect = new Rect(position.x, idRect.y + lineH + Spacing, position.width, lineH);
                EditorGUI.HelpBox(helpRect, $"AudioLibrary not found at Resources/{LibraryResourcePath}", MessageType.Warning);
            }
            else
            {
                DrawGroupDropdown(groupRect, groupProp, idProp, _cachedLibrary);
                DrawIdDropdown(idRect, groupProp, idProp, _cachedLibrary);
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

        private static void RefreshCache()
        {
            _cachedLibrary = Resources.Load<AudioLibrary>(LibraryResourcePath);
            _cacheInitialized = true;
        }

        /// <summary>
        /// Invalidates the cached library whenever assets change.
        /// Next OnGUI repaint will re-cache with a single Resources.Load call.
        /// </summary>
        private class CacheInvalidator : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                _cacheInitialized = false;
                _cachedLibrary = null;
            }
        }
    }
}
#endif
