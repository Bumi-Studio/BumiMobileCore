using System;
using System.IO;

namespace BumiMobile
{
    /// <summary>Publishes a flushed temporary file without truncating the previous save.</summary>
    public static class AtomicFile
    {
        public static void Write(string path, byte[] contents, bool createOnly = false)
        {
            string fullPath = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(fullPath);
            Directory.CreateDirectory(directory);
            string temporaryPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (FileStream stream = new FileStream(temporaryPath, FileMode.CreateNew,
                    FileAccess.Write, FileShare.None))
                {
                    stream.Write(contents, 0, contents.Length);
                    stream.Flush(true);
                }

                if (!createOnly && File.Exists(fullPath))
                {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                    // File.Replace is not implemented consistently by mobile runtimes.
                    // Publish through a backup so a failed move can restore the
                    // previous known-good save.
                    ReplaceWithMove(temporaryPath, fullPath);
#else
                    File.Replace(temporaryPath, fullPath, null);
#endif
                }
                else
                {
                    File.Move(temporaryPath, fullPath);
                }
            }
            finally
            {
                // Cleanup must not turn an already committed write into a reported failure.
                try { File.Delete(temporaryPath); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        private static void ReplaceWithMove(string temporaryPath, string fullPath)
        {
            string backupPath = fullPath + ".backup-" + Guid.NewGuid().ToString("N");
            File.Move(fullPath, backupPath);
            try
            {
                File.Move(temporaryPath, fullPath);
                try { File.Delete(backupPath); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            catch
            {
                if (!File.Exists(fullPath) && File.Exists(backupPath))
                    File.Move(backupPath, fullPath);
                throw;
            }
        }
#endif
    }
}
