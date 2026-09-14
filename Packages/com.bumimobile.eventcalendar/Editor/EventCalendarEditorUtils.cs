using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;

namespace BumiMobile
{
    internal static class EventCalendarEditorUtils
    {
        private const string DefaultDataRoot = "Assets/Project Files/Data";

        public static string EventCalendarRootFolder
        {
            get
            {
                string dataFolder = string.IsNullOrWhiteSpace(CoreEditor.FolderData) ? DefaultDataRoot : CoreEditor.FolderData;
                return NormalizeAssetPath(Path.Combine(dataFolder, "Event Calendar"));
            }
        }

        public static string EventDataFolder => NormalizeAssetPath(Path.Combine(EventCalendarRootFolder, "EventData"));
        public static string EventLibrariesFolder => NormalizeAssetPath(Path.Combine(EventCalendarRootFolder, "EventLibraries"));

        public static void EnsureFolderExists(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath))
                return;

            string normalizedPath = NormalizeAssetPath(assetFolderPath);
            string[] segments = normalizedPath.Split('/');
            string currentPath = segments[0];

            for (int i = 1; i < segments.Length; i++)
            {
                string nextPath = $"{currentPath}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(nextPath))
                    AssetDatabase.CreateFolder(currentPath, segments[i]);

                currentPath = nextPath;
            }
        }

        public static string NormalizeAssetPath(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? string.Empty : path.Replace("\\", "/");
        }

        public static string ToSafeAssetName(string rawValue, string fallback)
        {
            string source = string.IsNullOrWhiteSpace(rawValue) ? fallback : rawValue.Trim();
            char[] invalidChars = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalidChars.Length; i++)
            {
                source = source.Replace(invalidChars[i], '_');
            }

            return source.Replace(' ', '_');
        }

        public static string GenerateUniqueEventId(string rawValue, IReadOnlyCollection<string> existingIds = null)
        {
            string normalizedBase = NormalizeEventId(rawValue, "event");

            for (int attempt = 0; attempt < 8; attempt++)
            {
                string suffix = Guid.NewGuid().ToString("N").Substring(0, 12).ToLowerInvariant();
                string candidate = $"{normalizedBase}_{suffix}";

                if (TryValidateEventId(candidate, existingIds, out _))
                    return candidate;
            }

            return $"{normalizedBase}_{Guid.NewGuid():N}".ToLowerInvariant();
        }

        public static string NormalizeEventId(string rawValue, string fallback)
        {
            string source = ToSafeAssetName(rawValue, fallback).ToLowerInvariant();
            StringBuilder builder = new StringBuilder(source.Length);
            bool lastWasUnderscore = false;

            for (int i = 0; i < source.Length; i++)
            {
                char character = source[i];
                bool isValidCharacter = (character >= 'a' && character <= 'z') ||
                                        (character >= '0' && character <= '9');
                if (isValidCharacter)
                {
                    builder.Append(character);
                    lastWasUnderscore = false;
                }
                else if (!lastWasUnderscore)
                {
                    builder.Append('_');
                    lastWasUnderscore = true;
                }
            }

            string normalizedValue = builder.ToString().Trim('_');
            return string.IsNullOrWhiteSpace(normalizedValue) ? fallback : normalizedValue;
        }

        public static bool TryValidateEventId(string eventId, IReadOnlyCollection<string> existingIds, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(eventId))
            {
                error = "Event Id is required.";
                return false;
            }

            for (int i = 0; i < eventId.Length; i++)
            {
                char character = eventId[i];
                bool isValidCharacter = (character >= 'a' && character <= 'z') ||
                                        (character >= '0' && character <= '9') ||
                                        character == '_';
                if (!isValidCharacter)
                {
                    error = "Event Id must use lowercase letters, numbers, or underscore.";
                    return false;
                }
            }

            if (existingIds != null)
            {
                foreach (string existingId in existingIds)
                {
                    if (!string.Equals(existingId, eventId, StringComparison.OrdinalIgnoreCase))
                        continue;

                    error = $"Event Id '{eventId}' already exists in the database.";
                    return false;
                }
            }

            return true;
        }
    }
}
