using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace BumiMobile
{
    public static class SceneLoadingActionsMenu
    {
        [MenuItem("Actions/Game Scene", priority = 100)]
        private static void GameScene()
        {
            EditorSceneManager.OpenScene(Path.Combine(CoreEditor.FOLDER_SCENES, "Game.unity"));
        }

        [MenuItem("Actions/Game Scene", true)]
        private static bool GameSceneValidation()
        {
            return !Application.isPlaying;
        }
    }
}