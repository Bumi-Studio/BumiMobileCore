// Scripts/Auth/AuthenticatedInitModule.cs

using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BumiMobile
{
    [CreateAssetMenu(menuName = "BumiMobile/Auth/Authenticated Init Module", fileName = "AuthenticatedInitModule")]
    [RegisterModule("Authenticated", core: true, order: 0)]
    public class AuthenticatedInitModule : InitModule
    {
        private const string SkipAuthKey = "__reset_skip_auth__";
        private const string AuthDisabledKey = "__auth_disabled__";

        [Header("Timeout (seconds)")]
        [SerializeField] private float timeoutSeconds = 25f;
        [SerializeField] private CountryFlagDatabase countryFlags;

        [Header("Google Sign-In")]
        [SerializeField] private string googleWebClientId = AuthService.DEFAULT_GOOGLE_WEB_CLIENT_ID;
        [Tooltip("Show the Google account picker once when no account can be restored. A cancellation suppresses future automatic prompts.")]
        [SerializeField] private bool showPickerWhenNoAccount = true;

        public static bool IsAuthenticated { get; private set; }

        private bool initializationStarted;
        private bool initializationCompleted;
        private UniTaskCompletionSource initializationCompletionSource;

        public override string ModuleName => "Authenticated";
        public override bool IsAsync => Application.isPlaying;

        public override void CreateComponent()
        {
            if (Application.isPlaying) EnsureInitializationAsync().Forget();
        }

        public override IEnumerator InitializeCoroutine(Initializer initializer)
        {
            if (Application.isPlaying) yield return EnsureInitializationAsync().ToCoroutine();
        }

        private UniTask EnsureInitializationAsync()
        {
            if (!initializationStarted)
            {
                initializationStarted = true;
                initializationCompletionSource = new UniTaskCompletionSource();
                InitializeAsync().Forget();
            }

            return initializationCompleted ? UniTask.CompletedTask : initializationCompletionSource.Task;
        }

        private async UniTask InitializeAsync()
        {
            try
            {
                Debug.Log("[Auth] Init start");
                ConfigureAuthService();
                var countryInitTask = InitializeCountryAsync();

                if (PlayerPrefs.GetInt(AuthDisabledKey, 0) == 1)
                {
                    Debug.Log("[Auth] Auto sign-in disabled by user (persistent).");
                    IsAuthenticated = false;
                    await countryInitTask;
                    return;
                }

                if (PlayerPrefs.GetInt(SkipAuthKey, 0) == 1)
                {
                    PlayerPrefs.DeleteKey(SkipAuthKey);
                    PlayerPrefs.Save();
                    Debug.Log("[Auth] Skipping authentication after sign-out");
                    IsAuthenticated = false;
                    await countryInitTask;
                    return;
                }

                bool signInResult = false;
                CancellationTokenSource startupAuthCts = null;
                try
                {
                    if (timeoutSeconds > 0f)
                    {
                        startupAuthCts = new CancellationTokenSource();
                        startupAuthCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
                        signInResult = await AuthService.SignInAtStartupAsync(showPickerWhenNoAccount, startupAuthCts.Token);
                    }
                    else
                    {
                        signInResult = await AuthService.SignInAtStartupAsync(showPickerWhenNoAccount);
                    }
                }
                catch (OperationCanceledException) when (startupAuthCts != null && startupAuthCts.IsCancellationRequested)
                {
                    Debug.LogWarning("[Auth] Startup sign-in timed out; leaving the main menu unblocked.");
                    AuthService.SyncFromFirebaseCurrentUser();
                    signInResult = AuthService.IsSignedIn;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Auth] Sign-in encountered an exception: " + e.Message);
                }
                finally
                {
                    startupAuthCts?.Dispose();
                }

                await countryInitTask;
                IsAuthenticated = signInResult;
                Debug.Log($"[Auth] Init done | ok={IsAuthenticated} | id={AuthService.PlayerId} | country={CountryService.CountryISO}");
            }
            finally
            {
                initializationCompleted = true;
                initializationCompletionSource?.TrySetResult();
            }
        }

        private void ConfigureAuthService()
        {
            if (!string.IsNullOrEmpty(googleWebClientId)) AuthService.Initialize(googleWebClientId);
        }

        private UniTask InitializeCountryAsync()
        {
            var task = CountryService.InitializeAsync();
            return task.ContinueWith(() =>
            {
                if (countryFlags != null) CountryService.SetFlagDatabase(countryFlags);
                Debug.Log($"[Auth] Country resolved ISO={CountryService.CountryISO} Name={CountryService.CountryName}");
            });
        }
    }
}
