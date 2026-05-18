using UnityEngine;
using UnityEditor;
using System.IO;

namespace BumiMobile
{
    [CustomEditor(typeof(AudioInitModule))]
    public class AudioInitModuleEditor : InitModuleEditor
    {
        public override void OnCreated()
        {
            AudioLibrary audioLibrary = EditorUtils.GetAsset<AudioLibrary>();
            if (audioLibrary == null)
            {
                audioLibrary = (AudioLibrary)ScriptableObject.CreateInstance<AudioLibrary>();
                audioLibrary.name = "Audio Library";

                string referencePath = AssetDatabase.GetAssetPath(target);
                string directoryPath = Path.GetDirectoryName(referencePath);

                // Create a unique file path for the ScriptableObject
                string assetPath = Path.Combine(directoryPath, audioLibrary.name + ".asset");
                assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

                // Save the ScriptableObject to the determined path
                AssetDatabase.CreateAsset(audioLibrary, assetPath);
                AssetDatabase.SaveAssets();

                EditorUtility.SetDirty(target);
            }

            serializedObject.Update();
            serializedObject.FindProperty("audioSettings").objectReferenceValue = audioLibrary;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
