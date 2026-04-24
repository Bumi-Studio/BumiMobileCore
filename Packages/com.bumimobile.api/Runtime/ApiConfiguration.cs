using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Text;
#endif

namespace BumiMobile.Api
{
    [CreateAssetMenu(fileName = "ApiConfiguration", menuName = "BumiMobile/Api/ApiConfiguration")]
    public class ApiConfiguration : ScriptableObject
    {
        [Header("Base Configuration")]
        public string BaseUrl;

        [Tooltip("Timeout in seconds for API requests")]
        public float TimeoutSeconds = 30f;

        [Tooltip("Number of retry attempts for failed requests")]
        public int MaxRetries = 3;

        [Header("Routes")]
        public List<ApiRoute> Routes = new List<ApiRoute>();

        [Header("Code Generation")]
        public string GeneratedClassName = "ApiRoutes";
        public string GeneratedNamespace = "BumiMobile.Api";

        /// <summary>
        /// Create an ApiController instance from this configuration
        /// </summary>
        public ApiController CreateClient()
        {
            var settings = new ApiClientSettings
            {
                TimeoutSeconds = TimeoutSeconds,
                MaxRetries = MaxRetries
            };
            return new ApiController(BaseUrl, new NewtonsoftJsonSerializer(), settings);
        }

        public string GetRoute(string routeName)
        {
            var route = Routes.FirstOrDefault(r => r.Name == routeName);
            if (route.Name == null)
            {
                Debug.LogWarning($"Route '{routeName}' not found in ApiConfiguration");
                return string.Empty;
            }
            return route.Route;
        }

#if UNITY_EDITOR
        [ContextMenu("Generate Static API Class")]
        private void GenerateStaticClass()
        {
            if (Routes == null || Routes.Count == 0)
            {
                EditorUtility.DisplayDialog("No Routes", "Please add at least one route before generating the class.", "OK");
                return;
            }

            StringBuilder sb = new StringBuilder();

            // Get asset path for reference
            string assetPath = AssetDatabase.GetAssetPath(this);

            // Generate file header
            sb.AppendLine("// This file is auto-generated. Do not modify manually.");
            sb.AppendLine("// Generated from ApiConfiguration ScriptableObject");
            sb.AppendLine($"// Asset: {assetPath}");
            sb.AppendLine();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(GeneratedNamespace))
            {
                sb.AppendLine($"namespace {GeneratedNamespace}");
                sb.AppendLine("{");
            }

            int indent = string.IsNullOrEmpty(GeneratedNamespace) ? 0 : 1;
            string indentStr = new string(' ', indent * 4);

            // Generate class
            sb.AppendLine($"{indentStr}public static class {GeneratedClassName}");
            sb.AppendLine($"{indentStr}{{");
            sb.AppendLine($"{indentStr}    private static ApiConfiguration _config;");
            sb.AppendLine();
            sb.AppendLine($"{indentStr}    private static ApiConfiguration Config");
            sb.AppendLine($"{indentStr}    {{");
            sb.AppendLine($"{indentStr}        get");
            sb.AppendLine($"{indentStr}        {{");
            sb.AppendLine($"{indentStr}            if (_config == null)");
            sb.AppendLine($"{indentStr}            {{");
            sb.AppendLine($"{indentStr}                _config = Resources.Load<ApiConfiguration>(\"{Path.GetFileNameWithoutExtension(assetPath)}\");");
            sb.AppendLine($"{indentStr}                if (_config == null)");
            sb.AppendLine($"{indentStr}                {{");
            sb.AppendLine($"{indentStr}                    Debug.LogError(\"ApiConfiguration not found in Resources folder. Please move it to a Resources folder.\");");
            sb.AppendLine($"{indentStr}                }}");
            sb.AppendLine($"{indentStr}            }}");
            sb.AppendLine($"{indentStr}            return _config;");
            sb.AppendLine($"{indentStr}        }}");
            sb.AppendLine($"{indentStr}    }}");
            sb.AppendLine();
            sb.AppendLine($"{indentStr}    public static string BaseUrl => Config?.BaseUrl ?? string.Empty;");
            sb.AppendLine();
            sb.AppendLine($"{indentStr}    /// <summary>");
            sb.AppendLine($"{indentStr}    /// Create an ApiController instance from configuration");
            sb.AppendLine($"{indentStr}    /// </summary>");
            sb.AppendLine($"{indentStr}    public static ApiController CreateClient() => Config?.CreateClient();");
            sb.AppendLine();

            // Generate static properties for each route
            foreach (var route in Routes)
            {
                if (!string.IsNullOrEmpty(route.Name))
                {
                    string safeName = MakeSafeIdentifier(route.Name);
                    sb.AppendLine($"{indentStr}    public static string {safeName} => Config?.GetRoute(\"{route.Name}\") ?? string.Empty;");
                }
            }

            sb.AppendLine($"{indentStr}}}");

            if (!string.IsNullOrEmpty(GeneratedNamespace))
            {
                sb.AppendLine("}");
            }

            // Save file
            string directory = Path.GetDirectoryName(assetPath);
            string filePath = Path.Combine(directory, $"{GeneratedClassName}.cs");

            File.WriteAllText(filePath, sb.ToString());
            AssetDatabase.Refresh();

            Debug.Log($"Generated {GeneratedClassName}.cs at {filePath}");
            EditorUtility.DisplayDialog("Success", $"Generated {GeneratedClassName}.cs successfully!\n\nMake sure your ApiConfiguration is in a Resources folder for runtime access.", "OK");
        }

        private string MakeSafeIdentifier(string name)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    sb.Append(c);
                }
            }

            string result = sb.ToString();

            if (result.Length > 0 && char.IsDigit(result[0]))
            {
                result = "_" + result;
            }

            return string.IsNullOrEmpty(result) ? "Route" : result;
        }

        private void OnValidate()
        {
            // Check if generated file exists, if not auto-generate
            string assetPath = AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(assetPath)) return;

            string directory = Path.GetDirectoryName(assetPath);
            string filePath = Path.Combine(directory, $"{GeneratedClassName}.cs");

            if (!File.Exists(filePath) && Routes != null && Routes.Count > 0)
            {
                GenerateStaticClass();
            }
        }
#endif
    }

    [Serializable]
    public struct ApiRoute
    {
        public string Name;
        public string Route;
    }
}
