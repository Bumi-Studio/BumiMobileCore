using System;
using UnityEditor;
using UnityEngine;

namespace BumiMobile
{
    internal sealed class CreateEventPopupWindow : PopupWindowContent
    {
        private readonly Func<string, string, string> onCreate;
        private readonly Func<string, string> generateUniqueEventId;

        private string eventId = string.Empty;
        private string displayName = string.Empty;
        private string errorMessage = string.Empty;

        public CreateEventPopupWindow(Func<string, string, string> onCreate, Func<string, string> generateUniqueEventId)
        {
            this.onCreate = onCreate;
            this.generateUniqueEventId = generateUniqueEventId;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(320f, 138f);
        }

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.LabelField("Create Event", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            displayName = EditorGUILayout.TextField("Display Name", displayName);

            using (new EditorGUILayout.HorizontalScope())
            {
                eventId = EditorGUILayout.TextField("Event Id", eventId);
                if (GUILayout.Button("Generate", GUILayout.Width(72f)))
                {
                    string rawValue = string.IsNullOrWhiteSpace(displayName) ? eventId : displayName;
                    eventId = generateUniqueEventId != null ? generateUniqueEventId(rawValue) : eventId;
                    GUI.FocusControl(null);
                }
            }

            if (!string.IsNullOrWhiteSpace(errorMessage))
                EditorGUILayout.HelpBox(errorMessage, MessageType.Warning);

            GUILayout.FlexibleSpace();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Create", GUILayout.Width(88f)))
                {
                    errorMessage = onCreate != null ? onCreate(eventId, displayName) : "No create callback assigned.";
                    if (string.IsNullOrEmpty(errorMessage))
                        editorWindow.Close();
                }
            }
        }
    }
}
