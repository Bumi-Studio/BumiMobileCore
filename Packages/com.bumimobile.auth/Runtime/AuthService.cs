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
        // ---- Identity / State ----
        /// <summary>Google Sign-In user id (populated when Google Sign-In succeeds).</summary>
        public static string PlayerId { get; private set; } = string.Empty;

        public static Firebase.Auth.FirebaseUser User { get; private set; }
        public static bool IsFirebaseAnonymous => User != null && User.IsAnonymous;
        /// <summary>UI indicator: treat any Firebase user (including anonymous) as "logged in".</summary>
        public static bool IsAuthenticated => User != null;
        /// <summary>Strong sign-in (linked account): non-anonymous only.</summary>
        public static bool IsSignedIn => User != null && !User.IsAnonymous;

        static bool _busy;
        public static string LastAuthFailureReason { get; private set; } = string.Empty;

        /// <summary>
        /// OAuth 2.0 Web Client ID from Google Cloud Console.
        /// Must be set before calling SignInAsync / ManualSignInAsync.
        /// </summary>
        public static string WebClientId { get; set; }

        // Events
        public static event Action<bool> OnPlatformAuthFinished;
        public static event Action<Firebase.Auth.FirebaseUser> OnFirebaseAuthChanged;

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        /// <summary>
        /// Auto sign-in on app start. Does NOT attempt Google Sign-In —
        /// only initialises Firebase (restoring any persisted session).
        /// If no previous session exists, creates an anonymous Firebase account.
        /// Returns true when a non-anonymous Firebase user is signed in.
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
                return await InternalSignInAsync(interactive: false, manual: false);
            }
            finally { _busy = false; }
        }

        /// <summary>
        /// Manual variant for a "Sign in with Google" button.
        /// Shows the Google account picker if needed.
        /// </summary>
        public static async UniTask<bool> ManualSignInAsync()
        {
            if (_busy) return false;
            _busy = true;
            try
            {
                return await InternalSignInAsync(interactive: true, manual: true);
            }
            finally { _busy = false; }
        }

        public static async UniTask<bool> SignOutAsync()
        {
            try
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
                    var auth = FirebaseAuth.DefaultInstance;
                    auth?.SignOut();
                }

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

        /// <param name="interactive">When true, may show Google account picker UI.</param>
        /// <param name="manual">True when user explicitly tapped "Sign In" button.</param>
        static async UniTask<bool> InternalSignInAsync(bool interactive, bool manual)
        {
            // ---- Auto start: no Google Sign-In at all, just Firebase ----
            if (!manual)
            {
                Debug.Log("[Auth] Auto-init: skipping Google Sign-In, using Firebase (restore / anonymous).");
                await EnsureFirebaseAnonIfPossibleAsync();
                // Return true if Firebase restored a linked (non-anonymous) account
                return User != null && !User.IsAnonymous;
            }

            // ---- Manual "Sign in with Google" button ----
#if BUMI_AUTH_HAS_GOOGLE_SIGNIN
            if (string.IsNullOrEmpty(WebClientId))
            {
                Debug.LogError("[Auth] Google Sign-In WebClientId not configured. " +
                               "Set AuthService.WebClientId before calling sign-in.");
                LastAuthFailureReason = "WebClientId not configured";
                await EnsureFirebaseAnonIfPossibleAsync();
                return false;
            }

            bool platformOk = await GoogleSignInAuthenticateAsync(interactive: true);
            if (!platformOk)
            {
                Debug.LogWarning("[Auth] Google Sign-In failed – keeping existing Firebase session.");
                // Already have an anonymous session from earlier init; keep it
                return false;
            }

            PlayerId = GoogleSignIn.DefaultInstance.CurrentUser?.UserId ?? string.Empty;
            if (string.IsNullOrEmpty(PlayerId))
                Debug.LogWarning("[Auth] Google Sign-In returned empty user id.");

            bool firebaseOk = await TryLoginOrLinkFirebaseWithGoogleAsync();
            if (!firebaseOk)
            {
                Debug.LogWarning("[Auth] Firebase sign-in/link with Google failed – keeping existing session.");
                return false;
            }
            return true;

#else
            Debug.Log("[Auth] Google Sign-In SDK not available; keeping existing Firebase session.");
            return false;
#endif
        }

        // ====================================================================
        // LABEL
        // ====================================================================

        public static string GetUserLabel()
        {
#if BUMI_AUTH_HAS_GOOGLE_SIGNIN
            var googleUser = GoogleSignIn.DefaultInstance.CurrentUser;
            if (!string.IsNullOrEmpty(googleUser?.DisplayName)) return googleUser.DisplayName;
            if (!string.IsNullOrEmpty(googleUser?.Email)) return googleUser.Email;
#endif

            // Firebase fallback
            if (!string.IsNullOrEmpty(User?.DisplayName)) return User.DisplayName;
            if (!string.IsNullOrEmpty(User?.Email)) return User.Email;

            if (!string.IsNullOrEmpty(PlayerId)) return $"Player {PlayerId}";
            return string.Empty;
        }

        // ====================================================================
        // Google Sign-In
        // ====================================================================

#if BUMI_AUTH_HAS_GOOGLE_SIGNIN
        /// <summary>
        /// Google Sign-In platform authentication.
        /// When interactive=true, shows the account picker UI if no prior sign-in exists.
        /// When interactive=false (silent), only succeeds if user previously signed in.
        /// </summary>
        static async UniTask<bool> GoogleSignInAuthenticateAsync(bool interactive)
        {
            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                RequestIdToken = true,
                WebClientId = WebClientId,
                RequestEmail = true,
                RequestAuthCode = false,
                UseGameSignIn = false
            };

            try
            {
                GoogleSignInUser googleUser;
                if (interactive)
                {
                    // Manual: show account picker if needed
                    googleUser = await GoogleSignIn.DefaultInstance.SignIn();
                }
                else
                {
                    // Auto: try silent sign-in first, don't show UI
                    googleUser = await GoogleSignIn.DefaultInstance.SignInSilently();
                }

                if (googleUser == null)
                {
                    LastAuthFailureReason = "[Auth] Google Sign-In returned null user";
                    Debug.LogWarning(LastAuthFailureReason);
                    OnPlatformAuthFinished?.Invoke(false);
                    return false;
                }

                Debug.Log($"[Auth] Google Sign-In OK: {googleUser.UserId} / {googleUser.DisplayName}");
                OnPlatformAuthFinished?.Invoke(true);
                return true;
            }
            catch (Exception e)
            {
                LastAuthFailureReason = "[Auth] Google Sign-In error: " + e.Message;
                Debug.LogWarning(LastAuthFailureReason);
                OnPlatformAuthFinished?.Invoke(false);
                return false;
            }
        }

        /// <summary>Firebase sign-in / link using Google Sign-In ID token.</summary>
        static async UniTask<bool> TryLoginOrLinkFirebaseWithGoogleAsync()
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

                var googleUser = GoogleSignIn.DefaultInstance.CurrentUser;
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
                    OnFirebaseAuthChanged?.Invoke(User);
                    return User != null;
                }

                if (auth.CurrentUser.IsAnonymous)
                {
                    // Upgrade anonymous account to Google-linked
                    await auth.CurrentUser.LinkWithCredentialAsync(cred);
                    await auth.CurrentUser.ReloadAsync();
                    User = auth.CurrentUser;
                    OnFirebaseAuthChanged?.Invoke(User);
                    return true;
                }

                // Re-sign with Google credential
                User = await auth.SignInWithCredentialAsync(cred);
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
#endif // BUMI_AUTH_HAS_GOOGLE_SIGNIN

        // ====================================================================
        // Firebase Anonymous Fallback
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
