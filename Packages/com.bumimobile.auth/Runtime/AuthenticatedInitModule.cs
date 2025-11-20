// Scripts/Auth/AuthenticatedInitModule.cs
// ------------------------------------------------------------
// AuthenticatedInitModule
// - ORDER = 0 (paling awal)
// - Async coroutine dengan timeout
// - Country bootstrap simplified (only flag database sprite assignment). Locale removed.
// - Jalankan AuthService.SignInAsync()
// - Expose status IsAuthenticated / PlayerName / PlayerId
// ------------------------------------------------------------

using System;
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

        public static bool IsAuthenticated { get; private set; }

        public override string ModuleName => "Authenticated";

        public override void CreateComponent()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            InitializeAsync().Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
            Debug.Log("[Auth] Init start");

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

            bool timedOut = false;
            bool signInResult = false;

            try
            {
                var signInTask = AuthService.SignInAsync(true);
                var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(1f, timeoutSeconds)));
                var winner = await UniTask.WhenAny(signInTask, timeoutTask);

                if (winner == 0)
                {
                    signInResult = await signInTask;
                }
                else
                {
                    timedOut = true;
                }
            }
            catch (Exception e)
            {
                timedOut = false;
                signInResult = false;
                Debug.LogWarning("[Auth] Sign-in encountered an exception: " + e.Message);
            }

            await countryInitTask;

            if (timedOut)
            {
                Debug.LogWarning("[Auth] Init timeout — continuing without authenticated session.");
                IsAuthenticated = false;
                return;
            }

            IsAuthenticated = signInResult;
            Debug.Log($"[Auth] Init done | ok={IsAuthenticated} | id={AuthService.PlayerId} | country={CountryService.CountryISO}");
        }

        private UniTask InitializeCountryAsync()
        {
            var task = CountryService.InitializeAsync();
            return task.ContinueWith(() =>
            {
                if (countryFlags != null)
                {
                    CountryService.SetFlagDatabase(countryFlags);
                }

                Debug.Log($"[Auth] Country resolved ISO={CountryService.CountryISO} Name={CountryService.CountryName}");
            });
        }
    }
}
