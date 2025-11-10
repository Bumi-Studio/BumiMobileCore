using UnityEditor;
using UnityEngine;
using System.IO;

namespace BumiMobile
{
    [CustomEditor(typeof(LocalizationInitModule))]
    public class LocalizationInitModuleEditor : InitModuleEditor
    {
        public override void OnCreated()
        {
            LocalizationSettings localizationSettings = EditorUtils.GetAsset<LocalizationSettings>();
            if(!localizationSettings)
            {
                localizationSettings = CreateInstance<LocalizationSettings>();
                localizationSettings.name = "Localization Settings";

                string referencePath = AssetDatabase.GetAssetPath(target);
                string directoryPath = Path.GetDirectoryName(referencePath);
                
                // Create a unique file path for the ScriptableObject
                string assetPath = Path.Combine(directoryPath, localizationSettings.name + ".asset");
                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
                if (!AssetDatabase.IsValidFolder(assetPath))
                {
                    Directory.CreateDirectory(Path.Combine(directoryPath + "/Localization"));
                }

                // Save the ScriptableObject to the determined path
                AssetDatabase.CreateAsset(localizationSettings, assetPath);
                AssetDatabase.SaveAssets();

                EditorUtility.SetDirty(target);
            }

            serializedObject.Update();
            serializedObject.FindProperty("localizationSettings").objectReferenceValue = localizationSettings;
            serializedObject.ApplyModifiedProperties();
        }
        
    }
}

