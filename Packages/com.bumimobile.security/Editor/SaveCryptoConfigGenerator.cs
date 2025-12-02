using System.IO;
using UnityEditor;
using UnityEngine;

namespace BumiMobile.Security.Editor
{
    public static class SaveCryptoConfigGenerator
    {
        private const string ConfigPath = "Assets/Project Files/Game/Scripts/Security/SaveCryptoConfig.cs";

        [MenuItem("Tools/Security/Generate SaveCryptoConfig")]
        public static void Generate()
        {
            if (!EditorUtility.DisplayDialog("Generate SaveCryptoConfig",
                "This will create/update a local SaveCryptoConfig.cs containing your AppSecret (NOT committed). Continue?",
                "Yes", "No")) return;

            string secret = System.Guid.NewGuid().ToString("N") + System.Guid.NewGuid().ToString("N");
            string dir = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string code = "// AUTO-GENERATED. DO NOT COMMIT.\n" +
                         "namespace Watermelon.Security { public static class SaveCryptoConfig { public static readonly string AppSecret = \"" + secret + "\"; } }\n";

            File.WriteAllText(ConfigPath, code);
            AssetDatabase.Refresh();
            Debug.Log("[SaveCrypto] SaveCryptoConfig.cs generated at: " + ConfigPath);
        }
    }
}
