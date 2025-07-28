using UnityEngine;
using UnityEditor;

namespace BumiMobile
{
    public class EditorLevelPlayContainer : EditorAdsContainer
    {
        public EditorLevelPlayContainer(string containerName, string propertyName) : base(containerName, propertyName)
        {
        }

        protected override void SpecialButtons()
        {
            GUILayout.Space(8);

            if (GUILayout.Button("Getting Started Guide", EditorCustomStyles.button))
            {
                Application.OpenURL(@"https://developers.is.com/ironsource-mobile/unity/levelplay-starter-kit/");
            }

            if (GUILayout.Button("Integration Testing", EditorCustomStyles.button))
            {
                Application.OpenURL(@"https://developers.is.com/ironsource-mobile/unity/unity-levelplay-test-suite/#step-1");
            }

            GUILayout.Space(8);

            EditorGUILayout.HelpBox("Tested with ironSource v8.4.1", MessageType.Info);
        }
    }
}