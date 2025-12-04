using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

#if BUMI_AUTH_HAS_FIREBASE
using Firebase;
using Firebase.Auth;
#else
using FirebaseUser = System.Object;
#endif

#if UNITY_ANDROID && BUMI_AUTH_HAS_GPGS && BUMI_AUTH_HAS_FIREBASE
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

#if BUMI_AUTH_HAS_FIREBASE
namespace BumiMobile
{
    public static class AuthService
    {
        // ---- Identity / State ----
        public static string PlayerId { get; private set; } = string.Empty; // PGS id
        public static FirebaseUser User { get; private set; }
        public static bool IsFirebaseAnonymous => User != null && User.IsAnonymous;
        // UI indicator: treat any Firebase user (including anonymous) as "logged in"
        public static bool IsAuthenticated => User != null;
        // Strong sign-in (linked account): non-anonymous only
        public static bool IsSignedIn => User != null && !User.IsAnonymous;

        static bool _busy;
        static bool _pgsActivated;
        public static string LastAuthFailureReason { get; private set; } = string.Empty;

        // Events
        public static event Action<bool> OnPgsAuthFinished;            // kept for compatibility (Android)
        public static event Action<FirebaseUser> OnFirebaseAuthChanged; // user (can be null)

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        /// <summary>
        /// Try platform sign-in (PGS on Android), then Firebase.
        /// Fallback to Firebase Anonymous on failure.
        /// Returns true when non-anonymous Firebase user is signed in.
        /// </summary>
        public static async UniTask<bool> SignInAsync(bool forceRefreshToken = true)
        {
            if (_busy) { Debug.LogWarning("[Auth] Sign-in already running"); return false; }
            _busy = true;
            try
            {
                return await InternalSignInAsync(forceRefreshToken, interactive: true, manual: false);
            }
            finally { _busy = false; }
        }

        /// <summary>
        /// Manual variant for a "Sign in" button.
        /// </summary>
        public static async UniTask<bool> ManualSignInAsync()
        {
            if (_busy) return false;
            _busy = true;
            try
            {
                return await InternalSignInAsync(forceRefreshToken: true, interactive: true, manual: true);
            }
            finally { _busy = false; }
        }

        public static async UniTask<bool> SignOutAsync()
        {
            try
            {
                var dep = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (dep == DependencyStatus.Available)
                {
                    var auth = FirebaseAuth.DefaultInstance;
                    auth?.SignOut();
                }

                // PGS v2: no explicit sign-out API (Android)

                PlayerId = string.Empty;
                User = null;
                OnFirebaseAuthChanged?.Invoke(null);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Auth] SignOut failed: " + e.Message);
                return false;
            }
        }

        // ====================================================================
        // CORE FLOW
        // ====================================================================

        static async UniTask<bool> InternalSignInAsync(bool forceRefreshToken, bool interactive, bool manual)
        {
#if UNITY_ANDROID && BUMI_AUTH_HAS_GPGS
            EnsurePgsActivated();
            bool platformOk = await PgsAuthenticateAsync(interactive, manual);
            if (!platformOk)
            {
                Debug.LogWarning("[Auth] PGS auth failed – fallback to anonymous Firebase.");
                await EnsureFirebaseAnonIfPossibleAsync();
                return false;
            }

            PlayerId = PlayGamesPlatform.Instance?.localUser?.id ?? string.Empty;
            if (string.IsNullOrEmpty(PlayerId))
                Debug.LogWarning("[Auth] PGS returned empty player id.");

            bool firebaseOk = await TryLoginOrLinkFirebaseWithPgsAsync(forceRefreshToken);
            if (!firebaseOk)
            {
                Debug.LogWarning("[Auth] Firebase sign-in/link with PGS failed – fallback anonymous.");
                await EnsureFirebaseAnonIfPossibleAsync();
                return false;
            }
            return true;

#else
#if UNITY_ANDROID
            Debug.Log("[Auth] Google Play Games SDK not available; using Firebase Anonymous fallback.");
#else
            Debug.Log("[Auth] Using Firebase Anonymous (no platform sign-in on this platform).");
#endif
            await EnsureFirebaseAnonIfPossibleAsync();
            return false;
#endif
        }

        // ====================================================================
        // LABEL
        // ====================================================================

        public static string GetUserLabel()
        {
            // Prefer Firebase displayName/email; fallback to platform identity
            var name = User?.DisplayName;
            var mail = User?.Email;
            if (!string.IsNullOrEmpty(name)) return name;
            if (!string.IsNullOrEmpty(mail)) return mail;

#if UNITY_ANDROID && BUMI_AUTH_HAS_GPGS
            var pgsName = PlayGamesPlatform.Instance?.localUser?.userName;
            if (!string.IsNullOrEmpty(pgsName)) return pgsName;
#endif

            if (!string.IsNullOrEmpty(PlayerId)) return $"Player {PlayerId}";
            return string.Empty;
        }

        // ====================================================================
        // ANDROID (PGS)
        // ====================================================================

#if UNITY_ANDROID && BUMI_AUTH_HAS_GPGS
        static void EnsurePgsActivated()
        {
            if (_pgsActivated) return;
            PlayGamesPlatform.DebugLogEnabled = true;
            PlayGamesPlatform.Activate();
            _pgsActivated = true;
        }

        static UniTask<bool> PgsAuthenticateAsync(bool interactive, bool manual = false)
        {
            var tcs = new UniTaskCompletionSource<bool>();

            void SetResult(SignInStatus s)
            {
                if (s != SignInStatus.Success)
                    LastAuthFailureReason = "PGS SignInStatus=" + s;
                tcs.TrySetResult(s == SignInStatus.Success);
                OnPgsAuthFinished?.Invoke(s == SignInStatus.Success);
            }

            try
            {
                if (PlayGamesPlatform.Instance == null)
                {
                    Debug.LogWarning("[Auth] PGS Instance null before authenticate.");
                    tcs.TrySetResult(false);
                }
                else
                {
                    if (manual)
                        PlayGamesPlatform.Instance.ManuallyAuthenticate(SetResult);
                    else
                        PlayGamesPlatform.Instance.Authenticate(SetResult);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Auth] PGS authenticate threw: " + e.Message);
                tcs.TrySetResult(false);
            }

            return tcs.Task;
        }

        static UniTask<string> RequestPgsServerAuthCodeAsync(bool forceRefreshToken)
        {
            var tcs = new UniTaskCompletionSource<string>();
            try
            {
                if (PlayGamesPlatform.Instance == null)
                {
                    tcs.TrySetException(new Exception("PGS Instance null"));
                }
                else
                {
                    PlayGamesPlatform.Instance.RequestServerSideAccess(
                        forceRefreshToken,
                        code =>
                        {
                            if (string.IsNullOrEmpty(code))
                                tcs.TrySetException(new Exception("Empty auth code"));
                            else
                                tcs.TrySetResult(code);
                        });
                }
            }
            catch (Exception e) { tcs.TrySetException(e); }
            return tcs.Task;
        }

        /// <summary>Firebase with PGS credential (Android)</summary>
        static async UniTask<bool> TryLoginOrLinkFirebaseWithPgsAsync(bool forceRefreshToken)
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
                if (auth == null) { Debug.LogWarning("[Auth] FirebaseAuth.DefaultInstance null."); return false; }

                // Get server auth code (with one retry forcing refresh)
                string authCode = null;
                Exception lastCodeErr = null;
                for (int attempt = 0; attempt < 2 && string.IsNullOrEmpty(authCode); attempt++)
                {
                    bool force = attempt == 1 ? true : forceRefreshToken;
                    try
                    {
                        authCode = await RequestPgsServerAuthCodeAsync(force);
                        Debug.Log("[Auth] Obtained PGS auth code: " + authCode);
                    }
                    catch (Exception e) { lastCodeErr = e; }
                }
                if (string.IsNullOrEmpty(authCode))
                {
                    LastAuthFailureReason = "[Auth] Empty PGS auth code: " + (lastCodeErr?.Message ?? "Unknown");
                    Debug.LogWarning(LastAuthFailureReason);
                    return false;
                }

                var cred = PlayGamesAuthProvider.GetCredential(authCode);
                if (cred == null) { LastAuthFailureReason = "[Auth] Null PGS credential"; return false; }

                if (auth.CurrentUser == null)
                {
                    User = await auth.SignInWithCredentialAsync(cred);
                    OnFirebaseAuthChanged?.Invoke(User);
                    return User != null;
                }

                if (auth.CurrentUser.IsAnonymous)
                {
                    await auth.CurrentUser.LinkWithCredentialAsync(cred); // upgrade anon
                    await auth.CurrentUser.ReloadAsync();
                    User = auth.CurrentUser;
                    OnFirebaseAuthChanged?.Invoke(User);
                    return true;
                }

                User = await auth.SignInWithCredentialAsync(cred); // re-sign
                OnFirebaseAuthChanged?.Invoke(User);
                return User != null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Auth] Firebase PGS step failed: " + e.Message);
                LastAuthFailureReason = e.Message;
                return false;
            }
        }
#endif // UNITY_ANDROID && BUMI_AUTH_HAS_GPGS

        // ====================================================================
        // Firebase Anonymous Fallback (Common)
        // ====================================================================

        static async UniTask<bool> EnsureFirebaseAnonIfPossibleAsync()
        {
            try
            {
                var dep = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (dep != DependencyStatus.Available)
                {
                    Debug.LogWarning($"[Auth] Firebase deps not available for anonymous: {dep}");
                    return false;
                }

                var auth = FirebaseAuth.DefaultInstance;
                if (auth.CurrentUser != null)
                {
                    User = auth.CurrentUser;
                    return true;
                }

                var res = await auth.SignInAnonymouslyAsync();
                User = res?.User;
                Debug.Log($"[Auth] Firebase Anonymous OK: {User?.UserId}");
                OnFirebaseAuthChanged?.Invoke(User);
                return User != null;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Auth] Anonymous sign-in failed: " + e.Message);
                return false;
            }
        }
    }
}
#else
namespace BumiMobile
{
    public static class AuthService
    {
        public static string PlayerId { get; private set; } = string.Empty;
        public static FirebaseUser User => null;
        public static bool IsFirebaseAnonymous => true;
        public static bool IsAuthenticated => false;
        public static bool IsSignedIn => false;
        public static string LastAuthFailureReason { get; private set; } = "Firebase SDK missing. Define BUMI_AUTH_HAS_FIREBASE after importing Firebase packages.";

        public static event Action<bool> OnPgsAuthFinished;
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
            LastAuthFailureReason = "Firebase SDK missing. Define BUMI_AUTH_HAS_FIREBASE after importing Firebase packages.";
            Debug.LogWarning("[Auth] Firebase SDK not detected. AuthService is running in stub mode.");
        }
    }
}
#endif
