using UnityEngine;
using UnityEditor;

namespace BumiMobile
{
    public class EditorUnityAdsContainer : EditorAdsContainer
    {
        public EditorUnityAdsContainer(string containerName, string propertyName) : base(containerName, propertyName)
        {
        }

        protected override void SpecialButtons()
        {
            GUILayout.Space(8);

            if (GUILayout.Button("Unity Ads Dashboard", EditorCustomStyles.button))
            {
                Application.OpenURL(@"https://operate.dashboard.unity3d.com");
            }

            if (GUILayout.Button("Unity Ads Quick Start Guide", EditorCustomStyles.button))
            {
                Application.OpenURL(@"https://unityads.unity3d.com/help/monetization/getting-started");
            }

            GUILayout.Space(8);

            EditorGUILayout.HelpBox("Tested with Advertisement v4.12.0", MessageType.Info);
        }
    }
}