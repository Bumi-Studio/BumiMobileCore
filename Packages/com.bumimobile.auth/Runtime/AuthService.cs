using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

#if BUMI_AUTH_HAS_FIREBASE
using Firebase;
using Firebase.Auth;
#else
using FirebaseUser = System.Object;
#endif

#if BUMI_AUTH_HAS_GOOGLE_SIGNIN
using Google;
#endif

#if BUMI_AUTH_HAS_FIREBASE
namespace BumiMobile
{
    public static class AuthService
    {
        private const string PREF_PLAYER_ID = "__auth_player_id__";

        public static string PlayerId { get; private set; } = string.Empty;

        public static Firebase.Auth.FirebaseUser User { get; private set; }
        public static bool IsFirebaseAnonymous => User != null && User.IsAnonymous;
        public static bool IsAuthenticated => User != null;
        public static bool IsSignedIn => User != null && !User.IsAnonymous;

        public static string LastAuthFailureReason { get; private set; } = string.Empty;
        public static string WebClientId { get; set; }

        static bool _busy;

        public static event Action<bool> OnPlatformAuthFinished;
        public static event Action<Firebase.Auth.FirebaseUser> OnFirebaseAuthChanged;

        static AuthService()
        {
            PlayerId = PlayerPrefs.GetString(PREF_PLAYER_ID, string.Empty);
        }

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        /// <summary>
        /// Auto sign-in on app start.
        /// 1. Try silent Google Sign-In → link/upgrade Firebase.
        /// 2. Fall back to Firebase persisted session.
        /// 3. If no session, create anonymous account.
        /// Returns true when a non-anonymous (Google-linked) Firebase user is signed in.
        /// </summary>
        public static async UniTask<bool> SignInAsync(bool forceRefreshToken = true)
        {
            if (_busy)
            {
                Debug.LogWarning("[Auth] Sign-in already running");
                return false;
            }
            _busy = true;
            try
            {
                return await SignInAutoAsync();
            }
            finally { _busy = false; }
        }

        /// <summary>
        /// Manual "Sign in with Google" button.
        /// Shows the Google account picker, then links/upgrades Firebase.
        /// </summary>
        public static async UniTask<bool> ManualSignInAsync()
        {
            if (_busy)
            {
                Debug.LogWarning("[Auth] Manual sign-in already running");
                return false;
            }
            _busy = true;
            try
            {
                return await SignInManualAsync();
            }
            finally { _busy = false; }
        }

        /// <summary>
        /// Sign out of Google + Firebase, clear state, then immediately
        /// create a fresh anonymous account so the app always has a Firebase user.
        /// </summary>
        public static async UniTask<bool> SignOutAsync()
        {
            try
            {
                await SignOutAllAsync();
                return await CreateAnonymousAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Auth] SignOut failed: " + e.Message);
                return false;
            }
        }

        public static string GetUserLabel()
        {
            if (!string.IsNullOrEmpty(User?.DisplayName)) return User.DisplayName;
            if (!string.IsNullOrEmpty(User?.Email))       return User.Email;
            if (!string.IsNullOrEmpty(PlayerId))          return $"Player {PlayerId}";
            return string.Empty;
        }

        // ====================================================================
        // AUTO SIGN-IN
        // ====================================================================

        static async UniTask<bool> SignInAutoAsync()
        {
            var dep = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dep != DependencyStatus.Available)
            {
                Debug.LogWarning($"[Auth] Firebase deps: {dep}");
                return false;
            }

#if BUMI_AUTH_HAS_GOOGLE_SIGNIN
            if (!string.IsNullOrEmpty(WebClientId))
            {
                var googleUser = await GoogleSignInAuthenticateAsync(interactive: false);
                if (googleUser != null)
                {
                    if (await TryAuthFirebaseWithGoogleAsync(googleUser))
                    {
                        PersistPlayerId(googleUser.UserId);
                        return true;
                    }

                    Debug.LogWarning("[Auth] Firebase Google auth failed, falling back to Firebase session.");
                }
            }
#endif

            var auth = FirebaseAuth.DefaultInstance;
            if (auth.CurrentUser != null)
            {
                User = auth.CurrentUser;
                OnFirebaseAuthChanged?.Invoke(User);
                return !User.IsAnonymous;
            }

            return await CreateAnonymousAsync();
        }

        // ====================================================================
        // MANUAL SIGN-IN
        // ====================================================================

#if BUMI_AUTH_HAS_GOOGLE_SIGNIN
        static async UniTask<bool> SignInManualAsync()
        {
            if (string.IsNullOrEmpty(WebClientId))
            {
                Debug.LogError("[Auth] WebClientId not configured. Set AuthService.WebClientId before sign-in.");
                LastAuthFailureReason = "WebClientId not configured";
                return false;
            }

            var dep = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dep != DependencyStatus.Available)
            {
                Debug.LogWarning($"[Auth] Firebase deps: {dep}");
                return false;
            }

            var googleUser = await GoogleSignInAuthenticateAsync(interactive: true);
            if (googleUser == null)
            {
                Debug.LogWarning("[Auth] Google Sign-In failed – keeping existing session.");
                return false;
            }

            if (await TryAuthFirebaseWithGoogleAsync(googleUser))
            {
                PersistPlayerId(googleUser.UserId);
                return true;
            }

            Debug.LogWarning("[Auth] Firebase Google auth failed – keeping existing session.");
            return false;
        }
#else
        static async UniTask<bool> SignInManualAsync()
        {
            Debug.Log("[Auth] Google Sign-In SDK not available.");
            return false;
        }
#endif

        // ====================================================================
        // SIGN OUT
        // ====================================================================

        static async UniTask SignOutAllAsync()
        {
#if BUMI_AUTH_HAS_GOOGLE_SIGNIN
            try
            {
                GoogleSignIn.DefaultInstance.SignOut();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Auth] Google Sign-Out error: " + ex.Message);
            }
#endif

            var dep = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dep == DependencyStatus.Available)
            {
                FirebaseAuth.DefaultInstance?.SignOut();
            }

            PlayerId = string.Empty;
            User = null;
            if (PlayerPrefs.HasKey(PREF_PLAYER_ID))
                PlayerPrefs.DeleteKey(PREF_PLAYER_ID);
            PlayerPrefs.Save();
            OnFirebaseAuthChanged?.Invoke(null);
        }

        // ====================================================================
        // GOOGLE SIGN-IN (platform)
        // ====================================================================

#if BUMI_AUTH_HAS_GOOGLE_SIGNIN
        static async UniTask<GoogleSignInUser> GoogleSignInAuthenticateAsync(bool interactive)
        {
            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                RequestIdToken  = true,
                WebClientId     = WebClientId,
                RequestEmail    = true,
                RequestAuthCode = false,
                UseGameSignIn   = false
            };

            try
            {
                var googleUser = interactive
                    ? await GoogleSignIn.DefaultInstance.SignIn()
                    : await GoogleSignIn.DefaultInstance.SignInSilently();

                if (googleUser == null)
                {
                    LastAuthFailureReason = "[Auth] Google Sign-In returned null user";
                    Debug.LogWarning(LastAuthFailureReason);
                    OnPlatformAuthFinished?.Invoke(false);
                    return null;
                }

                Debug.Log($"[Auth] Google Sign-In OK: {googleUser.UserId} / {googleUser.DisplayName}");
                OnPlatformAuthFinished?.Invoke(true);
                return googleUser;
            }
            catch (Exception e)
            {
                LastAuthFailureReason = "[Auth] Google Sign-In error: " + e.Message;
                Debug.LogWarning(LastAuthFailureReason);
                OnPlatformAuthFinished?.Invoke(false);
                return null;
            }
        }

        // ====================================================================
        // FIREBASE AUTH WITH GOOGLE CREDENTIAL
        // ====================================================================

        static async UniTask<bool> TryAuthFirebaseWithGoogleAsync(GoogleSignInUser googleUser)
        {
            try
            {
                var dep = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (dep != DependencyStatus.Available)
                {
                    Debug.LogWarning($"[Auth] Firebase deps: {dep}");
                    return false;
                }

                var auth = FirebaseAuth.DefaultInstance;
                if (auth == null)
                {
                    Debug.LogWarning("[Auth] FirebaseAuth.DefaultInstance null.");
                    return false;
                }

                if (googleUser == null || string.IsNullOrEmpty(googleUser.IdToken))
                {
                    LastAuthFailureReason = "[Auth] No Google IdToken available";
                    Debug.LogWarning(LastAuthFailureReason);
                    return false;
                }

                var cred = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);
                if (cred == null)
                {
                    LastAuthFailureReason = "[Auth] Null Google credential";
                    return false;
                }

                if (auth.CurrentUser == null)
                {
                    User = await auth.SignInWithCredentialAsync(cred);
                    if (User != null)
                        OnFirebaseAuthChanged?.Invoke(User);
                    return User != null;
                }

                if (auth.CurrentUser.IsAnonymous)
                {
                    await auth.CurrentUser.LinkWithCredentialAsync(cred);
                    await auth.CurrentUser.ReloadAsync();
                    User = auth.CurrentUser;
                    OnFirebaseAuthChanged?.Invoke(User);
                    return true;
                }

                User = await auth.SignInWithCredentialAsync(cred);
                if (User != null)
                    OnFirebaseAuthChanged?.Invoke(User);
                return User != null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Auth] Firebase Google step failed: " + e.Message);
                LastAuthFailureReason = e.Message;
                return false;
            }
        }
#endif

        // ====================================================================
        // ANONYMOUS
        // ====================================================================

        static async UniTask<bool> CreateAnonymousAsync()
        {
            try
            {
                var dep = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (dep != DependencyStatus.Available)
                {
                    Debug.LogWarning($"[Auth] Firebase deps for anonymous: {dep}");
                    return false;
                }

                var auth = FirebaseAuth.DefaultInstance;
                var res = await auth.SignInAnonymouslyAsync();
                User = res?.User;
                if (User != null)
                {
                    Debug.Log($"[Auth] Anonymous OK: {User.UserId}");
                    OnFirebaseAuthChanged?.Invoke(User);
                    return true;
                }

                Debug.LogWarning("[Auth] Anonymous sign-in returned null user.");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Auth] Anonymous sign-in failed: " + e.Message);
                return false;
            }
        }

        // ====================================================================
        // PLAYER PREFS
        // ====================================================================

        static void PersistPlayerId(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
                return;

            PlayerId = playerId;
            PlayerPrefs.SetString(PREF_PLAYER_ID, playerId);
            PlayerPrefs.Save();
        }
    }
}
#else
// ====================================================================
// STUB: Firebase SDK not present
// ====================================================================
namespace BumiMobile
{
    public static class AuthService
    {
        public static string PlayerId { get; private set; } = string.Empty;
        public static FirebaseUser User => null;
        public static bool IsFirebaseAnonymous => true;
        public static bool IsAuthenticated => false;
        public static bool IsSignedIn => false;
        public static string LastAuthFailureReason { get; private set; } =
            "Firebase SDK missing. Define BUMI_AUTH_HAS_FIREBASE after importing Firebase packages.";

        public static string WebClientId { get; set; }

        public static event Action<bool> OnPlatformAuthFinished;
        public static event Action<FirebaseUser> OnFirebaseAuthChanged;

        public static UniTask<bool> SignInAsync(bool forceRefreshToken = true)
        {
            LogStubWarning();
            return UniTask.FromResult(false);
        }

        public static UniTask<bool> ManualSignInAsync()
        {
            LogStubWarning();
            return UniTask.FromResult(false);
        }

        public static UniTask<bool> SignOutAsync()
        {
            LogStubWarning();
            return UniTask.FromResult(false);
        }

        public static string GetUserLabel() => string.Empty;

        static void LogStubWarning()
        {
            LastAuthFailureReason =
                "Firebase SDK missing. Define BUMI_AUTH_HAS_FIREBASE after importing Firebase packages.";
            Debug.LogWarning("[Auth] Firebase SDK not detected. AuthService is running in stub mode.");
        }
    }
}
#endif
