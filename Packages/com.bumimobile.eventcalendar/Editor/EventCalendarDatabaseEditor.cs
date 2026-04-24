using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BumiMobile
{
    [CustomEditor(typeof(EventCalendarDatabase))]
    public class EventCalendarDatabaseEditor : Editor
    {
        private SerializedProperty eventsProperty;
        private EventCalendarData existingEventToAdd;
        private int selectedEventIndex = -1;

        private void OnEnable()
        {
            eventsProperty = serializedObject.FindProperty("events");
            EnsureSelectedEventIndexValid();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EnsureSelectedEventIndexValid();

            HashSet<string> duplicateIds = CollectDuplicateEventIds();

            DrawScriptField();
            DrawAuthoringHub();
            DrawEventList(duplicateIds);
            DrawSelectedEvent(duplicateIds);
            DrawDatabaseValidation(duplicateIds);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawScriptField()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Script", MonoScript.FromScriptableObject((ScriptableObject)target), typeof(MonoScript), false);
            }
        }

        private void DrawAuthoringHub()
        {
            EditorGUILayoutCustom.BeginBoxGroup("Authoring Hub");
            EditorGUILayout.HelpBox("Create and manage local event assets from this inspector. Init resolves one active scheduled event from local UTC time and swaps sprites from the linked EventSpriteLibrary.", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                Rect createButtonRect = GUILayoutUtility.GetRect(new GUIContent("Create Event"), GUI.skin.button, GUILayout.Height(24f));
                if (GUI.Button(createButtonRect, "Create Event"))
                {
                    PopupWindow.Show(createButtonRect, new CreateEventPopupWindow(TryCreateEvent, rawValue => EventCalendarEditorUtils.GenerateUniqueEventId(rawValue, CollectExistingEventIds())));
                }
            }

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("Add Existing Event", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                existingEventToAdd = (EventCalendarData)EditorGUILayout.ObjectField(existingEventToAdd, typeof(EventCalendarData), false);
                using (new EditorGUI.DisabledScope(existingEventToAdd == null))
                {
                    if (GUILayout.Button("Add Existing", GUILayout.Width(110f)))
                    {
                        if (AddExistingEvent(existingEventToAdd))
                            existingEventToAdd = null;
                    }
                }
            }

            EditorGUILayoutCustom.EndBoxGroup();
        }

        private void DrawEventList(HashSet<string> duplicateIds)
        {
            EditorGUILayoutCustom.BeginBoxGroup($"Event List ({eventsProperty.arraySize})");

            if (eventsProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No events registered in the database.", MessageType.Info);
                EditorGUILayoutCustom.EndBoxGroup();
                return;
            }

            for (int i = 0; i < eventsProperty.arraySize; i++)
            {
                SerializedProperty element = eventsProperty.GetArrayElementAtIndex(i);
                EventCalendarData eventData = element.objectReferenceValue as EventCalendarData;
                string title = eventData == null ? $"Event Slot {i + 1}" : eventData.DisplayName;
                int warningCount = CollectEventListWarnings(eventData, duplicateIds).Count;
                string status = eventData == null ? "Missing asset" : warningCount == 0 ? "Ready" : $"{warningCount} warning{(warningCount == 1 ? string.Empty : "s")}";

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(status, EditorStyles.miniLabel, GUILayout.Width(100f));

                using (new EditorGUI.DisabledScope(selectedEventIndex == i))
                {
                    if (GUILayout.Button(selectedEventIndex == i ? "Selected" : "Select", GUILayout.Width(70f)))
                        selectedEventIndex = i;
                }

                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    TryRemoveEventFromList(i, eventData);
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField(eventData == null ? "Assign an EventCalendarData asset or remove this slot." : $"Id: {eventData.EventId}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayoutCustom.EndBoxGroup();
        }

        private void DrawSelectedEvent(HashSet<string> duplicateIds)
        {
            EditorGUILayoutCustom.BeginBoxGroup("Selected Event");

            if (eventsProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("Create or add an event to start editing details.", MessageType.Info);
                EditorGUILayoutCustom.EndBoxGroup();
                return;
            }

            if (selectedEventIndex < 0 || selectedEventIndex >= eventsProperty.arraySize)
            {
                EditorGUILayout.HelpBox("Select an event from the list above.", MessageType.Info);
                EditorGUILayoutCustom.EndBoxGroup();
                return;
            }

            SerializedProperty element = eventsProperty.GetArrayElementAtIndex(selectedEventIndex);
            EventCalendarData eventData = element.objectReferenceValue as EventCalendarData;

            if (eventData == null)
                DrawEmptySelectedEventSlot(selectedEventIndex, element);
            else
                DrawSelectedEventDetails(selectedEventIndex, eventData, duplicateIds);

            EditorGUILayoutCustom.EndBoxGroup();
        }

        private void DrawEmptySelectedEventSlot(int index, SerializedProperty element)
        {
            EditorGUILayout.HelpBox($"Event slot {index + 1} is empty. Assign an existing asset or remove the slot.", MessageType.Warning);

            EditorGUI.BeginChangeCheck();
            EventCalendarData assignedEvent = (EventCalendarData)EditorGUILayout.ObjectField("Event Asset", element.objectReferenceValue, typeof(EventCalendarData), false);
            if (EditorGUI.EndChangeCheck())
            {
                element.objectReferenceValue = assignedEvent;
                EditorUtility.SetDirty(target);
            }

            if (GUILayout.Button("Remove Empty Slot", GUILayout.Height(24f)))
            {
                RemoveEventAtIndex(index);
                GUIUtility.ExitGUI();
            }
        }

        private void DrawSelectedEventDetails(int index, EventCalendarData eventData, HashSet<string> duplicateIds)
        {
            SerializedObject eventSerializedObject = new SerializedObject(eventData);
            SerializedProperty displayNameProperty = eventSerializedObject.FindProperty("displayName");
            SerializedProperty eventIdProperty = eventSerializedObject.FindProperty("eventId");
            SerializedProperty spriteLibraryProperty = eventSerializedObject.FindProperty("spriteLibrary");
            SerializedProperty scheduleProperty = eventSerializedObject.FindProperty("schedule");

            eventSerializedObject.Update();

            EventSpriteLibrary spriteLibrary = spriteLibraryProperty.objectReferenceValue as EventSpriteLibrary;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Event Asset", eventData, typeof(EventCalendarData), false);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Event Asset", GUILayout.Height(24f)))
                {
                    Selection.activeObject = eventData;
                    EditorGUIUtility.PingObject(eventData);
                }

                using (new EditorGUI.DisabledScope(spriteLibrary == null))
                {
                    if (GUILayout.Button("Open Sprite Library", GUILayout.Height(24f)))
                    {
                        Selection.activeObject = spriteLibrary;
                        EditorGUIUtility.PingObject(spriteLibrary);
                    }
                }

                if (GUILayout.Button("Remove From Database", GUILayout.Height(24f)))
                {
                    RemoveEventAtIndex(index);
                    eventSerializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUI.BeginChangeCheck();
            displayNameProperty.stringValue = EditorGUILayout.DelayedTextField("Display Name", displayNameProperty.stringValue);
            using (new EditorGUILayout.HorizontalScope())
            {
                eventIdProperty.stringValue = EditorGUILayout.DelayedTextField("Event Id", eventIdProperty.stringValue);
                if (GUILayout.Button("Generate", GUILayout.Width(72f)))
                {
                    string baseLabel = !string.IsNullOrWhiteSpace(displayNameProperty.stringValue)
                        ? displayNameProperty.stringValue
                        : string.IsNullOrWhiteSpace(eventIdProperty.stringValue) ? eventData.name : eventIdProperty.stringValue;
                    eventIdProperty.stringValue = EventCalendarEditorUtils.GenerateUniqueEventId(baseLabel, CollectExistingEventIds(eventData));
                    GUI.FocusControl(null);
                }
            }

            EditorGUILayout.PropertyField(spriteLibraryProperty, new GUIContent("Sprite Library"));
            bool scheduleChanged = EventCalendarScheduleEditorGUI.DrawScheduleSection(
                scheduleProperty,
                "Local Schedule",
                "Init resolves the active event from local UTC time at startup.");

            bool eventChanged = EditorGUI.EndChangeCheck();
            if (eventChanged || scheduleChanged)
            {
                eventSerializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(eventData);
                spriteLibrary = spriteLibraryProperty.objectReferenceValue as EventSpriteLibrary;
            }
            else
            {
                eventSerializedObject.ApplyModifiedProperties();
            }

            if (spriteLibrary == null)
            {
                EditorGUILayout.HelpBox("Sprite Library is missing.", MessageType.Warning);
                if (GUILayout.Button("Create Missing Sprite Library", GUILayout.Height(24f)))
                    CreateMissingSpriteLibrary(eventData);
            }
            else
            {
                EventSpriteLibraryEditorGUI.DrawLibraryInspector(spriteLibrary, false, "Sprite Entries");
            }

            DrawEventWarnings(eventData, spriteLibrary, duplicateIds);
        }

        private void DrawEventWarnings(EventCalendarData eventData, EventSpriteLibrary spriteLibrary, HashSet<string> duplicateIds)
        {
            List<string> warnings = CollectEventWarnings(eventData, spriteLibrary, duplicateIds);
            if (warnings.Count == 0)
                return;

            EditorGUILayoutCustom.BeginBoxGroup("Event Validation");
            for (int i = 0; i < warnings.Count; i++)
            {
                EditorGUILayout.HelpBox(warnings[i], MessageType.Warning);
            }
            EditorGUILayoutCustom.EndBoxGroup();
        }

        private List<string> CollectEventWarnings(EventCalendarData eventData, EventSpriteLibrary spriteLibrary, HashSet<string> duplicateIds)
        {
            List<string> warnings = new List<string>();

            if (eventData == null)
            {
                warnings.Add("Event asset is missing.");
                return warnings;
            }

            if (string.IsNullOrWhiteSpace(eventData.EventId))
            {
                warnings.Add("Event Id should not be empty.");
            }
            else
            {
                if (!EventCalendarEditorUtils.TryValidateEventId(eventData.EventId, null, out string eventIdError))
                    warnings.Add(eventIdError);

                if (duplicateIds.Contains(eventData.EventId))
                    warnings.Add($"Duplicate Event Id '{eventData.EventId}'.");
            }

            if (spriteLibrary == null)
            {
                warnings.Add("Sprite Library is empty or invalid.");
            }
            else
            {
                List<string> libraryWarnings = EventSpriteLibraryEditorGUI.CollectValidationWarnings(spriteLibrary);
                for (int i = 0; i < libraryWarnings.Count; i++)
                    warnings.Add(libraryWarnings[i]);
            }

            List<string> scheduleWarnings = EventCalendarScheduleUtils.CollectScheduleWarnings(eventData);
            for (int i = 0; i < scheduleWarnings.Count; i++)
                warnings.Add(scheduleWarnings[i]);

            List<string> overlapWarnings = CollectScheduleOverlapWarnings(eventData);
            for (int i = 0; i < overlapWarnings.Count; i++)
                warnings.Add(overlapWarnings[i]);

            return warnings;
        }

        private List<string> CollectEventListWarnings(EventCalendarData eventData, HashSet<string> duplicateIds)
        {
            List<string> warnings = new List<string>();

            if (eventData == null)
            {
                warnings.Add("Event asset is missing.");
                return warnings;
            }

            if (string.IsNullOrWhiteSpace(eventData.EventId))
                warnings.Add("Event Id should not be empty.");
            else if (duplicateIds.Contains(eventData.EventId))
                warnings.Add($"Duplicate Event Id '{eventData.EventId}'.");

            if (!eventData.HasValidSpriteLibraryReference)
                warnings.Add("Sprite Library is missing.");

            List<string> scheduleWarnings = EventCalendarScheduleUtils.CollectScheduleWarnings(eventData);
            for (int i = 0; i < scheduleWarnings.Count; i++)
                warnings.Add(scheduleWarnings[i]);

            return warnings;
        }

        private void DrawDatabaseValidation(HashSet<string> duplicateIds)
        {
            List<string> warnings = CollectDatabaseWarnings(duplicateIds);
            if (warnings.Count == 0)
                return;

            EditorGUILayoutCustom.BeginBoxGroup("Database Validation");
            for (int i = 0; i < warnings.Count; i++)
            {
                EditorGUILayout.HelpBox(warnings[i], MessageType.Warning);
            }
            EditorGUILayoutCustom.EndBoxGroup();
        }

        private List<string> CollectDatabaseWarnings(HashSet<string> duplicateIds)
        {
            List<string> warnings = new List<string>();
            HashSet<string> reportedDuplicateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < eventsProperty.arraySize; i++)
            {
                EventCalendarData eventData = eventsProperty.GetArrayElementAtIndex(i).objectReferenceValue as EventCalendarData;
                if (eventData == null)
                {
                    warnings.Add($"Event slot {i + 1} is empty.");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(eventData.EventId) &&
                    duplicateIds.Contains(eventData.EventId) &&
                    reportedDuplicateIds.Add(eventData.EventId))
                {
                    warnings.Add($"Duplicate Event Id '{eventData.EventId}'.");
                }
            }

            List<EventCalendarScheduleConflict> conflicts = EventCalendarScheduleUtils.CollectScheduleConflicts(((EventCalendarDatabase)target).Events);
            for (int i = 0; i < conflicts.Count; i++)
            {
                EventCalendarScheduleConflict conflict = conflicts[i];
                warnings.Add(
                    $"Scheduled events '{EventCalendarScheduleUtils.GetEventLabel(conflict.FirstEvent)}' and '{EventCalendarScheduleUtils.GetEventLabel(conflict.SecondEvent)}' overlap. Overlaps are not allowed.");
            }

            return warnings;
        }

        private List<string> CollectScheduleOverlapWarnings(EventCalendarData eventData)
        {
            List<string> warnings = new List<string>();
            if (!EventCalendarScheduleUtils.TryGetValidScheduleRange(eventData, out DateTime eventStartUtc, out DateTime eventEndUtc))
                return warnings;

            EventCalendarData[] databaseEvents = ((EventCalendarDatabase)target).Events;
            for (int i = 0; i < databaseEvents.Length; i++)
            {
                EventCalendarData otherEvent = databaseEvents[i];
                if (otherEvent == null || otherEvent == eventData)
                    continue;

                if (!EventCalendarScheduleUtils.TryGetValidScheduleRange(otherEvent, out DateTime otherStartUtc, out DateTime otherEndUtc))
                    continue;

                bool overlaps = eventStartUtc <= otherEndUtc && otherStartUtc <= eventEndUtc;
                if (!overlaps)
                    continue;

                warnings.Add($"Schedule overlaps with '{EventCalendarScheduleUtils.GetEventLabel(otherEvent)}'.");
            }

            return warnings;
        }

        private HashSet<string> CollectDuplicateEventIds()
        {
            HashSet<string> seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> duplicateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < eventsProperty.arraySize; i++)
            {
                EventCalendarData eventData = eventsProperty.GetArrayElementAtIndex(i).objectReferenceValue as EventCalendarData;
                if (eventData == null || string.IsNullOrWhiteSpace(eventData.EventId))
                    continue;

                if (!seenIds.Add(eventData.EventId))
                    duplicateIds.Add(eventData.EventId);
            }

            return duplicateIds;
        }

        private string TryCreateEvent(string eventId, string displayName)
        {
            HashSet<string> existingIds = CollectExistingEventIds();

            string normalizedId = string.IsNullOrWhiteSpace(eventId)
                ? EventCalendarEditorUtils.GenerateUniqueEventId(displayName, existingIds)
                : eventId.Trim();

            if (!EventCalendarEditorUtils.TryValidateEventId(normalizedId, existingIds, out string validationError))
                return validationError;

            if (string.IsNullOrWhiteSpace(displayName))
                displayName = ObjectNames.NicifyVariableName(normalizedId);

            EventCalendarEditorUtils.EnsureFolderExists(EventCalendarEditorUtils.EventDataFolder);
            EventCalendarEditorUtils.EnsureFolderExists(EventCalendarEditorUtils.EventLibrariesFolder);

            string eventAssetName = EventCalendarEditorUtils.ToSafeAssetName($"Event_{normalizedId}", "Event");
            string eventAssetPath = AssetDatabase.GenerateUniqueAssetPath($"{EventCalendarEditorUtils.EventDataFolder}/{eventAssetName}.asset");

            string libraryAssetName = EventCalendarEditorUtils.ToSafeAssetName($"EventSpriteLibrary_{normalizedId}", "EventSpriteLibrary");
            string libraryAssetPath = AssetDatabase.GenerateUniqueAssetPath($"{EventCalendarEditorUtils.EventLibrariesFolder}/{libraryAssetName}.asset");

            EventCalendarData eventAsset = CreateInstance<EventCalendarData>();
            AssetDatabase.CreateAsset(eventAsset, eventAssetPath);

            EventSpriteLibrary libraryAsset = CreateInstance<EventSpriteLibrary>();
            AssetDatabase.CreateAsset(libraryAsset, libraryAssetPath);

            SerializedObject eventSerializedObject = new SerializedObject(eventAsset);
            eventSerializedObject.Update();
            eventSerializedObject.FindProperty("eventId").stringValue = normalizedId;
            eventSerializedObject.FindProperty("displayName").stringValue = displayName;
            eventSerializedObject.FindProperty("spriteLibrary").objectReferenceValue = libraryAsset;
            eventSerializedObject.ApplyModifiedProperties();

            EditorUtility.SetDirty(eventAsset);
            EditorUtility.SetDirty(libraryAsset);

            AddEventAsset(eventAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = eventAsset;
            EditorGUIUtility.PingObject(eventAsset);

            return string.Empty;
        }

        private HashSet<string> CollectExistingEventIds(EventCalendarData excludedEvent = null)
        {
            HashSet<string> existingIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < eventsProperty.arraySize; i++)
            {
                EventCalendarData existingEvent = eventsProperty.GetArrayElementAtIndex(i).objectReferenceValue as EventCalendarData;
                if (existingEvent == null || existingEvent == excludedEvent || string.IsNullOrWhiteSpace(existingEvent.EventId))
                    continue;

                existingIds.Add(existingEvent.EventId);
            }

            return existingIds;
        }

        private void CreateMissingSpriteLibrary(EventCalendarData eventData)
        {
            if (eventData == null)
                return;

            string eventId = string.IsNullOrWhiteSpace(eventData.EventId)
                ? EventCalendarEditorUtils.ToSafeAssetName(eventData.name, "event")
                : eventData.EventId;

            EventCalendarEditorUtils.EnsureFolderExists(EventCalendarEditorUtils.EventLibrariesFolder);

            string libraryAssetName = EventCalendarEditorUtils.ToSafeAssetName($"EventSpriteLibrary_{eventId}", "EventSpriteLibrary");
            string libraryAssetPath = AssetDatabase.GenerateUniqueAssetPath($"{EventCalendarEditorUtils.EventLibrariesFolder}/{libraryAssetName}.asset");

            EventSpriteLibrary libraryAsset = CreateInstance<EventSpriteLibrary>();
            AssetDatabase.CreateAsset(libraryAsset, libraryAssetPath);

            SerializedObject serializedEvent = new SerializedObject(eventData);
            serializedEvent.Update();
            serializedEvent.FindProperty("spriteLibrary").objectReferenceValue = libraryAsset;
            serializedEvent.ApplyModifiedProperties();

            EditorUtility.SetDirty(eventData);
            EditorUtility.SetDirty(libraryAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private bool AddExistingEvent(EventCalendarData eventData)
        {
            if (eventData == null)
                return false;

            if (ContainsEventAsset(eventData))
            {
                EditorUtility.DisplayDialog("Already Added", $"{eventData.name} is already registered in the database.", "OK");
                return false;
            }

            AddEventAsset(eventData);
            return true;
        }

        private void AddEventAsset(EventCalendarData eventData)
        {
            serializedObject.Update();
            eventsProperty.arraySize += 1;
            eventsProperty.GetArrayElementAtIndex(eventsProperty.arraySize - 1).objectReferenceValue = eventData;
            serializedObject.ApplyModifiedProperties();

            selectedEventIndex = eventsProperty.arraySize - 1;
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }

        private bool ContainsEventAsset(EventCalendarData eventData)
        {
            for (int i = 0; i < eventsProperty.arraySize; i++)
            {
                if (eventsProperty.GetArrayElementAtIndex(i).objectReferenceValue == eventData)
                    return true;
            }

            return false;
        }

        private void TryRemoveEventFromList(int index, EventCalendarData eventData)
        {
            string eventLabel = eventData == null ? $"Event slot {index + 1}" : eventData.DisplayName;
            string message = eventData == null
                ? $"Remove {eventLabel} from the database list?"
                : $"Remove '{eventLabel}' from the database list?\n\nThis only removes it from EventCalendarDatabase. The asset file will stay in the project.";

            if (!EditorUtility.DisplayDialog("Remove Event", message, "Remove", "Cancel"))
                return;

            RemoveEventAtIndex(index);
        }

        private void RemoveEventAtIndex(int index)
        {
            if (index < 0 || index >= eventsProperty.arraySize)
                return;

            eventsProperty.RemoveFromVariableArrayAt(index);
            serializedObject.ApplyModifiedProperties();

            if (eventsProperty.arraySize == 0)
            {
                selectedEventIndex = -1;
            }
            else if (selectedEventIndex > index)
            {
                selectedEventIndex -= 1;
            }
            else if (selectedEventIndex == index)
            {
                selectedEventIndex = Mathf.Min(index, eventsProperty.arraySize - 1);
            }

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }

        private void EnsureSelectedEventIndexValid()
        {
            if (eventsProperty == null)
                return;

            if (eventsProperty.arraySize == 0)
            {
                selectedEventIndex = -1;
                return;
            }

            if (selectedEventIndex < 0 || selectedEventIndex >= eventsProperty.arraySize)
                selectedEventIndex = Mathf.Clamp(selectedEventIndex, 0, eventsProperty.arraySize - 1);
        }
    }
}
