using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BumiMobile
{
    public static class GameLoading
    {
        private const float MinimumLoadingTime = 2.0f;

        private static AsyncOperation loadingOperation;
        private static bool isReadyToHide;
        private static bool manualControlMode;
        private static string loadingMessage;
        private static float manualProgress = -1f;
        private static readonly List<LoadingTask> loadingTasks = new List<LoadingTask>();

        public static event LoadingCallback OnLoading;
        public static event Action OnLoadingFinished;

        public static void SetLoadingMessage(string message)
        {
            loadingMessage = message;

            float progress = loadingOperation != null ? loadingOperation.progress : 0.0f;
            OnLoading?.Invoke(ResolveProgress(progress), message);
        }

        public static void SetLoadingStatus(float progress, string message)
        {
            manualProgress = Mathf.Clamp01(progress);
            loadingMessage = message;

            float fallback = loadingOperation != null ? loadingOperation.progress : 0.0f;
            OnLoading?.Invoke(ResolveProgress(fallback), message);
        }

        public static void ClearManualProgressOverride()
        {
            manualProgress = -1f;
        }

        public static void AddTask(LoadingTask loadingTask)
        {
            loadingTasks.Add(loadingTask);
        }

        private static IEnumerator LoadSceneCoroutine(SimpleCallback onSceneLoaded = null)
        {
            isReadyToHide = false;

            float realtimeSinceStartup = Time.realtimeSinceStartup;

            int taskIndex = 0;
            while (taskIndex < loadingTasks.Count)
            {
                if (!loadingTasks[taskIndex].IsActive)
                {
                    loadingTasks[taskIndex].Activate();
                    SetLoadingMessage($"Loading {loadingTasks[taskIndex].TaskName}...");
                }

                if (loadingTasks[taskIndex].IsFinished)
                {
                    taskIndex++;
                }

                yield return null;
            }

            int sceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (SceneManager.sceneCount < sceneIndex)
                Debug.LogError("[Loading]: First scene is missing!");

            float minimumFinishTime = realtimeSinceStartup + MinimumLoadingTime;

            ClearManualProgressOverride();

            loadingOperation = SceneManager.LoadSceneAsync(sceneIndex);
            loadingOperation.allowSceneActivation = false;

            while (!loadingOperation.isDone || realtimeSinceStartup < minimumFinishTime)
            {
                yield return null;

                realtimeSinceStartup = Time.realtimeSinceStartup;

                float currentProgress = loadingOperation != null ? loadingOperation.progress : 1.0f;
                OnLoading?.Invoke(ResolveProgress(currentProgress), loadingMessage);

                if (loadingOperation.progress >= 0.9f)
                {
                    loadingOperation.allowSceneActivation = true;
                }
            }

            if (manualControlMode)
            {
                float manualWarningTime = Time.realtimeSinceStartup + 10.0f;
                bool warningLogged = false;

                while (!isReadyToHide)
                {
                    if (!warningLogged && Time.realtimeSinceStartup >= manualWarningTime)
                    {
                        Debug.LogError("[Loading]: Seems like you forget to call MarkAsReadyToHide method to finish the loading process.");
                        warningLogged = true;
                    }

                    yield return null;
                }
            }

            OnLoading?.Invoke(ResolveProgress(1.0f), "Done");

            yield return null;

            onSceneLoaded?.Invoke();
            OnLoadingFinished?.Invoke();

            loadingOperation = null;
            ClearManualProgressOverride();
        }

        private static IEnumerator SimpleLoadCoroutine(SimpleCallback onSceneLoaded = null)
        {
            int taskIndex = 0;
            while (taskIndex < loadingTasks.Count)
            {
                if (!loadingTasks[taskIndex].IsActive)
                {
                    loadingTasks[taskIndex].Activate();
                }

                if (loadingTasks[taskIndex].IsFinished)
                {
                    taskIndex++;
                }

                yield return null;
            }

            onSceneLoaded?.Invoke();
            OnLoadingFinished?.Invoke();
        }

        public static void MarkAsReadyToHide()
        {
            isReadyToHide = true;
        }

        public static void EnableManualControlMode()
        {
            manualControlMode = true;
        }

        public static void LoadGameScene(SimpleCallback onSceneLoaded = null)
        {
            Initializer.RunCoroutine(LoadSceneCoroutine(onSceneLoaded));
        }

        public static void SimpleLoad(SimpleCallback onSceneLoaded = null)
        {
            Initializer.RunCoroutine(SimpleLoadCoroutine(onSceneLoaded));
        }

        public delegate void LoadingCallback(float state, string message);

        private static float ResolveProgress(float fallback)
        {
            return manualProgress >= 0f ? manualProgress : fallback;
        }
    }
}
