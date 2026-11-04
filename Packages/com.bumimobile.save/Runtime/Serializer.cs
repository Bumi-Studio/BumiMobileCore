using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace BumiMobile
{
    public static class Serializer
    {
        private static string persistentDataPath;

        public static void Init()
        {
            persistentDataPath = Application.persistentDataPath;
        }

        /// <summary>
        /// Deserializes file located at Persistent Data Path.
        /// </summary>
        /// <param name="fileName">Name of input file.</param>
        /// <returns>Deserialized object if file exists or new instance if doesn't.</returns>
        public static T Deserialize<T>(string fileName, bool logIfFileNotExists = false) where T : new()
        {
            string absolutePath = Path.Combine(GetPersistentDataPath(), fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));

            try
            {
                using (FileStream file = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    object value = new BinaryFormatter().Deserialize(file);
                    if (value is T result) return result;
                    throw new InvalidDataException("The save contains an unexpected root object.");
                }
            }
            catch (FileNotFoundException)
            {
                if (logIfFileNotExists) Debug.LogWarning("Save file does not exist: " + absolutePath);
                return new T();
            }
            catch (Exception exception)
            {
                if (!TryQuarantine(absolutePath, out string quarantinePath))
                {
                    Debug.LogError("Save data is corrupt and could not be quarantined: " + absolutePath);
                    throw;
                }

                Debug.LogError("Save data was corrupt and was moved to " + quarantinePath + ". " + exception.Message);
                return new T();
            }
        }

        /// <summary>
        /// Serializes file to Persistent Data Path.
        /// </summary>
        /// <param name="fileName">Name of output file.</param>
        /// <param name="objectToSerialize">Reference to object that should be serialized.</param>
        public static void Serialize<T>(T objectToSerialize, string fileName)
        {
            // Preserve the existing format, but finish serialization before touching the destination.
            using (MemoryStream stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, objectToSerialize);
                AtomicFile.Write(Path.Combine(GetPersistentDataPath(), fileName), stream.ToArray());
            }
        }

        /// <summary>
        /// Checks if file exists at Persistent Data Path.
        /// </summary>
        /// <param name="fileName">Name of file to check.</param>
        /// <returns>True if file exists ans false otherwise.</returns>
        public static bool FileExistsAtPDP(string fileName)
        {
            return File.Exists(Path.Combine(GetPersistentDataPath(), fileName));
        }

        /// <summary>
        /// Checks if file exists at Persistent Data Path.
        /// </summary>
        /// <param name="absolutePath">Absolute path to file(including file name and extention.</param>
        /// <returns>True if file exists ans false otherwise.</returns>
        public static bool FileExistsAtPath(string absolutePath)
        {
            return File.Exists(absolutePath);
        }

        /// <summary>
        /// Checks if file exists add specified directory.
        /// </summary>
        /// <param name="directoryPath">Full path to directory. Ends with directory name (without "/").</param>
        /// <param name="fileName">Name of file to check.</param>
        /// <returns>True if file exists and false otherwise.</returns>
        public static bool FileExistsAtPath(string directoryPath, string fileName)
        {
            return File.Exists(Path.Combine(directoryPath, fileName));
        }

        /// <summary>
        /// Delete file at Persistent Data Path.
        /// </summary>
        /// <param name="fileName">Name of file to check.</param>
        public static void DeleteFileAtPDP(string fileName)
        {
            File.Delete(Path.Combine(GetPersistentDataPath(), fileName));
        }

        /// <summary>
        /// Delete file at specified path.
        /// </summary>
        /// <param name="absolutePath">Absolute path to file(including file name and extention.</param>
        public static void DeleteFileAtPath(string absolutePath)
        {
            File.Delete(absolutePath);
        }

        /// <summary>
        /// Delete file at specified path.
        /// </summary>
        /// <param name="fileName">Name of file to check.</param>
        /// <param name="directoryPath">Full path to directory. Ends with directory name (without "/").</param>
        public static void DeleteFileAtPath(string directoryPath, string fileName)
        {
            File.Delete(Path.Combine(directoryPath, fileName));
        }

        private static string GetPersistentDataPath()
        {
            if (string.IsNullOrEmpty(persistentDataPath))
            {
                persistentDataPath = Application.persistentDataPath;

                return persistentDataPath;
            }

            return persistentDataPath;
        }

        private static bool TryQuarantine(string absolutePath, out string quarantinePath)
        {
            quarantinePath = absolutePath + ".corrupt-" +
                DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "-" +
                Guid.NewGuid().ToString("N");
            try
            {
                File.Move(absolutePath, quarantinePath);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Could not quarantine corrupt save: " + exception.GetType().Name);
                quarantinePath = null;
                return false;
            }
        }
    }
}
