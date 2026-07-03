using System;
using System.Threading;
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
        private const string PREF_USER_EXPLICITLY_SIGNED_OUT = "__auth_explicitly_signed_out__";
        public const string DEFAULT_GOOGLE_WEB_CLIENT_ID =
            "1098096377753-1u3n2oias158bhpg2jpficvh7dt42b5h.apps.googleusercontent.com";

        public static string PlayerId { get; private set; } = string.Empty;

        public static Firebase.Auth.FirebaseUser User { get; private set; }
        public static bool IsFirebaseAnonymous => User != null && User.IsAnonymous;
        public static bool IsAuthenticated => User != null;
        public static bool IsSignedIn => User != null && !User.IsAnonymous;

        public static string LastAuthFailureReason { get; private set; } = string.Empty;

        /// <summary>
        /// OAuth 2.0 Web Client ID from Google Cloud Console.
        /// Set once via <see cref="Initialize"/> before any sign-in call.
        /// </summary>
        public static string WebClientId { get; private set; }

        public static bool IsExplicitlySignedOut =>
            PlayerPrefs.GetInt(PREF_USER_EXPLICITLY_SIGNED_OUT, 0) == 1;

        static bool _busy;
        static FirebaseAuth _firebaseAuthInstance;
        static bool _firebaseAuthStateListenerAttached;

        /// <summary>
        /// Initializes the auth service with required configuration.
        /// Must be called once before any sign-in operation.
        /// </summary>
        /// <param name="webClientId">
        /// The OAuth 2.0 Web Client ID from Google Cloud Console
        /// (e.g. "123456789-abcdef.apps.googleusercontent.com").
        /// Pass null or empty to disable Google Sign-In entirely.
        /// </param>
        public static void Initialize(string webClientId)
        {
            if (string.Equals(WebClientId, webClientId, StringComparison.Ordinal))
            {
                return;
            }

            WebClientId = webClientId;
            _busy = false;
        }

        public static bool SyncFromFirebaseCurrentUser()
        {
            try
            {
                var auth = FirebaseAuth.DefaultInstance;
                if (auth == null)
                {
                    return false;
                }

                var currentUser = auth.CurrentUser;
                if (currentUser == null)
                {
                    if (User != null)
                    {
                        User = null;
                        OnFirebaseAuthChanged?.Invoke(null);
                    }

                    return false;
                }

                bool userChanged = User == null ||
                    !string.Equals(User.UserId, currentUser.UserId, StringComparison.Ordinal) ||
                    User.IsAnonymous != currentUser.IsAnonymous;

                User = currentUser;

                if (userChanged)
                {
                    OnFirebaseAuthChanged?.Invoke(User);
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Auth] Failed to sync Firebase session: " + e.Message);
                return false;
            }
        }

        static void EnsureFirebaseAuthStateListener(FirebaseAuth auth)
        {
            if (auth == null)
            {
                return;
            }

            if (_firebaseAuthStateListenerAttached && ReferenceEquals(_firebaseAuthInstance, auth))
            {
                return;
            }

            if (_firebaseAuthStateListenerAttached && _firebaseAuthInstance != null)
            {
                _firebaseAuthInstance.StateChanged -= OnFirebaseAuthStateChanged;
            }

            _firebaseAuthInstance = auth;
            _firebaseAuthInstance.StateChanged += OnFirebaseAuthStateChanged;
            _firebaseAuthStateListenerAttached = true;
        }

        static void OnFirebaseAuthStateChanged(object sender, EventArgs e)
        {
            SyncFromFirebaseCurrentUser();
        }

        public static event Action<bool> OnPlatformAuthFinished;

        /// <summary>
        /// Fires when the Firebase user changes (sign-in, sign-out, anonymous creation).
        /// After <see cref="SignOutAsync"/>, subscribers receive <c>null</c>.
        /// </summary>
        public static event Action<Firebase.Auth.FirebaseUser> OnFirebaseAuthChanged;

        /// <summary>
        /// [Obsolete] Use <see cref="OnPlatformAuthFinished"/> instead.
        /// </summary>
        [Obsolete("Use OnPlatformAuthFinished instead.")]
        public static event Action<bool> OnPgsAuthFinished
        {
            add => OnPlatformAuthFinished += value;
            remove => OnPlatformAuthFinished -= value;
        }

        static AuthService()
        {
            PlayerId = PlayerPrefs.GetString(PREF_PLAYER_ID, string.Empty);
        }

        // ====================================================================
        // PUBLIC API
        // ====================================================================

        /// <summary>
        /// Auto sign-in on app start.
        /// 1. Try silent Google Sign-In → link/upgrade Firebase (skipped if user explicitly signed out).
        /// 2. Fall back to Firebase persisted session.
        /// 3. If no session and the player has not explicitly signed out, create anonymous account.
        /// Returns true when a non-anonymous (Google-linked) Firebase user is signed in.
        /// </summary>
        public static async UniTask<bool> SignInAsync(CancellationToken cancellationToken = default)
        {
            if (_busy)
            {
                Debug.LogWarning("[Auth] Sign-in already running");
                return false;
            }
            _busy = true;
            try
            {
                return await SignInAutoAsync(cancellationToken);
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
        /// Sign out of Google + Firebase, clear state, and leave the app signed
        /// out until the player signs in again.
        ///
        /// Sets a PlayerPrefs flag so the NEXT cold start skips silent Google
        /// Sign-In — the user has explicitly chosen to sign out.
        /// </summary>
        public static async UniTask<bool> SignOutAsync()
        {
            try
            {
                await SignOutAllAsync();
                PlayerPrefs.SetInt(PREF_USER_EXPLICITLY_SIGNED_OUT, 1);
                PlayerPrefs.Save();
                return true;
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
            if (!string.IsNullOrEmpty(User?.Email)) return User.Email;
            if (!string.IsNullOrEmpty(PlayerId)) return $"Player {PlayerId}";
            if (!string.IsNullOrEmpty(User?.UserId)) return $"Guest {User.UserId.Substring(0, Math.Min(User.UserId.Length, 6))}";
            return string.Empty;
        }

        // ====================================================================
        // AUTO SIGN-IN
        // ====================================================================

        static async UniTask<bool> SignInAutoAsync(CancellationToken cancellationToken = default)
        {
            var dep = await FirebaseApp.CheckAndFixDependenciesAsync()
                .AsUniTask()
                .AttachExternalCancellation(cancellationToken);
            if (dep != DependencyStatus.Available)
            {
                Debug.LogWarning($"[Auth] Firebase deps: {dep}");
                return false;
            }

            // If the user explicitly signed out last session, skip silent
            // Google Sign-In to avoid auto-re-authenticating them.
            bool userExplicitlySignedOut = IsExplicitlySignedOut;
            var auth = FirebaseAuth.DefaultInstance;
            EnsureFirebaseAuthStateListener(auth);

#if BUMI_AUTH_HAS_GOOGLE_SIGNIN
            if (!userExplicitlySignedOut && !string.IsNullOrEmpty(WebClientId))
            {
                var googleUser = await GoogleSignInAuthenticateAsync(interactive: false, cancellationToken);
                if (googleUser != null)
                {
                    // Clear explicit-sign-out flag — the user is now authenticated
                    if (PlayerPrefs.HasKey(PREF_USER_EXPLICITLY_SIGNED_OUT))
                    {
                        PlayerPrefs.DeleteKey(PREF_USER_EXPLICITLY_SIGNED_OUT);
                        PlayerPrefs.Save();
                    }

                    if (await TryAuthFirebaseWithGoogleAsync(googleUser, cancellationToken))
                    {
                        PersistPlayerId(googleUser.UserId);
                        return true;
                    }

                    Debug.LogWarning("[Auth] Firebase Google auth failed, falling back to Firebase session.");
                }
            }
#else
            if (userExplicitlySignedOut)
            {
                Debug.Log("[Auth] Explicit sign-out active. Skipping automatic anonymous sign-in.");
            }
#endif

            if (userExplicitlySignedOut)
            {
                try
                {
                    auth?.SignOut();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[Auth] Explicit sign-out cleanup failed: " + ex.Message);
                }

                User = null;
                OnFirebaseAuthChanged?.Invoke(null);
                return false;
            }

            if (SyncFromFirebaseCurrentUser())
            {
                return !User.IsAnonymous;
            }

            return await CreateAnonymousAsync(cancellationToken);
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

            EnsureFirebaseAuthStateListener(FirebaseAuth.DefaultInstance);

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
                if (EnsureGoogleConfigured())
                {
                    GoogleSignIn.DefaultInstance.SignOut();
                }
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
        private static GoogleSignInConfiguration _cachedGoogleConfig;
        private static string _cachedWebClientIdForConfig;

        static bool EnsureGoogleConfigured()
        {
            if (string.IsNullOrEmpty(WebClientId))
            {
                LastAuthFailureReason = "WebClientId not configured";
                Debug.LogWarning("[Auth] Google Sign-In skipped because WebClientId is not configured.");
                return false;
            }

            if (_cachedGoogleConfig == null || _cachedWebClientIdForConfig != WebClientId)
            {
                _cachedGoogleConfig = new GoogleSignInConfiguration
                {
                    RequestIdToken = true,
                    WebClientId = WebClientId,
                    RequestEmail = true,
                    RequestAuthCode = false,
                    UseGameSignIn = false
                };
                _cachedWebClientIdForConfig = WebClientId;
            }

            try
            {
                GoogleSignIn.Configuration = _cachedGoogleConfig;
                return true;
            }
            catch (Exception e)
            {
                LastAuthFailureReason = "[Auth] Google Sign-In configuration error: " + e.Message;
                Debug.LogWarning(LastAuthFailureReason);
                return false;
            }
        }

        static async UniTask<GoogleSignInUser> GoogleSignInAuthenticateAsync(bool interactive, CancellationToken cancellationToken = default)
        {
            if (!EnsureGoogleConfigured())
            {
                OnPlatformAuthFinished?.Invoke(false);
                return null;
            }

            // Cache configuration — only rebuild when WebClientId changes
            if (_cachedGoogleConfig == null || _cachedWebClientIdForConfig != WebClientId)
            {
                _cachedGoogleConfig = new GoogleSignInConfiguration
                {
                    RequestIdToken = true,
                    WebClientId = WebClientId,
                    RequestEmail = true,
                    RequestAuthCode = false,
                    UseGameSignIn = false
                };
                _cachedWebClientIdForConfig = WebClientId;
            }

            GoogleSignIn.Configuration = _cachedGoogleConfig;

            try
            {
                var googleUser = interactive
                    ? await GoogleSignIn.DefaultInstance.SignIn()
                        .AsUniTask()
                        .AttachExternalCancellation(cancellationToken)
                    : await GoogleSignIn.DefaultInstance.SignInSilently()
                        .AsUniTask()
                        .AttachExternalCancellation(cancellationToken);

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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                // Attempt to distinguish cancellation from real errors
                string msg = e is OperationCanceledException || e.Message?.Contains("cancel", StringComparison.OrdinalIgnoreCase) == true
                    ? "User cancelled Google Sign-In"
                    : "[Auth] Google Sign-In error: " + e.Message;

                LastAuthFailureReason = msg;
                Debug.LogWarning(msg);
                OnPlatformAuthFinished?.Invoke(false);
                return null;
            }
        }

        // ====================================================================
        // FIREBASE AUTH WITH GOOGLE CREDENTIAL
        // ====================================================================

        static async UniTask<bool> TryAuthFirebaseWithGoogleAsync(GoogleSignInUser googleUser, CancellationToken cancellationToken = default)
        {
            try
            {
                var dep = await FirebaseApp.CheckAndFixDependenciesAsync()
                    .AsUniTask()
                    .AttachExternalCancellation(cancellationToken);
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
                    User = await auth.SignInWithCredentialAsync(cred)
                        .AsUniTask()
                        .AttachExternalCancellation(cancellationToken);
                    if (User != null)
                        OnFirebaseAuthChanged?.Invoke(User);
                    return User != null;
                }

                if (auth.CurrentUser.IsAnonymous)
                {
                    // await auth.CurrentUser.LinkWithCredentialAsync(cred);
                    // await auth.CurrentUser.ReloadAsync();
                    // User = auth.CurrentUser;
                    // OnFirebaseAuthChanged?.Invoke(User);
                    // return true;

                    try
                    {
                        await auth.CurrentUser.LinkWithCredentialAsync(cred)
                            .AsUniTask()
                            .AttachExternalCancellation(cancellationToken);
                        await auth.CurrentUser.ReloadAsync()
                            .AsUniTask()
                            .AttachExternalCancellation(cancellationToken);
                        User = auth.CurrentUser;
                        OnFirebaseAuthChanged?.Invoke(User);
                        return true;
                    }
                    catch (Exception e) when (IsCredentialAlreadyInUseError(e))
                    {
                        // Credential already linked to a different Firebase account.
                        // Sign into that existing account, discarding the anonymous one.
                        Debug.Log("[Auth] Google credential already linked to another account. Signing into that account.");
                        User = await auth.SignInWithCredentialAsync(cred)
                            .AsUniTask()
                            .AttachExternalCancellation(cancellationToken);
                        if (User != null)
                        {
                            OnFirebaseAuthChanged?.Invoke(User);
                            return true;
                        }

                        LastAuthFailureReason = "[Auth] Sign-in to existing account returned null user.";
                        return false;
                    }
                }

                // IMPORTANT: When auth.CurrentUser is non-null AND non-anonymous,
                // SignInWithCredentialAsync creates a NEW Firebase user with a DIFFERENT UID.
                // Cloud data (Firestore, RTDB) tied to the old UID will be orphaned.
                // This typically happens when a user switches Google accounts.
                // Consider prompting the user before this path.
                User = await auth.SignInWithCredentialAsync(cred)
                    .AsUniTask()
                    .AttachExternalCancellation(cancellationToken);
                if (User != null)
                    OnFirebaseAuthChanged?.Invoke(User);
                return User != null;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
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

        static async UniTask<bool> CreateAnonymousAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var dep = await FirebaseApp.CheckAndFixDependenciesAsync()
                    .AsUniTask()
                    .AttachExternalCancellation(cancellationToken);
                if (dep != DependencyStatus.Available)
                {
                    Debug.LogWarning($"[Auth] Firebase deps for anonymous: {dep}");
                    OnPlatformAuthFinished?.Invoke(false);
                    return false;
                }

                var auth = FirebaseAuth.DefaultInstance;
                var res = await auth.SignInAnonymouslyAsync()
                    .AsUniTask()
                    .AttachExternalCancellation(cancellationToken);
                User = res?.User;
                if (User != null)
                {
                    Debug.Log($"[Auth] Anonymous OK: {User.UserId}");
                    OnFirebaseAuthChanged?.Invoke(User);
                    OnPlatformAuthFinished?.Invoke(false); // signal: platform auth skipped, using anonymous
                    return true;
                }

                Debug.LogWarning("[Auth] Anonymous sign-in returned null user.");
                OnPlatformAuthFinished?.Invoke(false);
                return false;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Auth] Anonymous sign-in failed: " + e.Message);
                OnPlatformAuthFinished?.Invoke(false);
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

            // Clear explicit-sign-out flag — user is now authenticated
            if (PlayerPrefs.HasKey(PREF_USER_EXPLICITLY_SIGNED_OUT))
                PlayerPrefs.DeleteKey(PREF_USER_EXPLICITLY_SIGNED_OUT);

            PlayerPrefs.Save();
        }

        private static bool IsCredentialAlreadyInUseError(Exception e)
        {
            if (e == null) return false;
            var msg = e.Message ?? string.Empty;
            if (msg.Contains("already associated")) return true;
            return IsCredentialAlreadyInUseError(e.InnerException);
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
        public const string DEFAULT_GOOGLE_WEB_CLIENT_ID =
            "1098096377753-1u3n2oias158bhpg2jpficvh7dt42b5h.apps.googleusercontent.com";

        public static string PlayerId { get; private set; } = string.Empty;
        public static FirebaseUser User => null;
        public static bool IsFirebaseAnonymous => true;
        public static bool IsAuthenticated => false;
        public static bool IsSignedIn => false;
        public static bool IsExplicitlySignedOut => PlayerPrefs.GetInt("__auth_explicitly_signed_out__", 0) == 1;
        public static string LastAuthFailureReason { get; private set; } =
            "Firebase SDK missing. Define BUMI_AUTH_HAS_FIREBASE after importing Firebase packages.";

        public static string WebClientId { get; private set; }

        public static void Initialize(string webClientId)
        {
            WebClientId = webClientId;
        }

        public static event Action<bool> OnPlatformAuthFinished;
        public static event Action<FirebaseUser> OnFirebaseAuthChanged;

        [Obsolete("Use OnPlatformAuthFinished instead.")]
        public static event Action<bool> OnPgsAuthFinished
        {
            add => OnPlatformAuthFinished += value;
            remove => OnPlatformAuthFinished -= value;
        }

        public static UniTask<bool> SignInAsync(CancellationToken cancellationToken = default)
        {
            LogStubWarning();
            return UniTask.FromResult(false);
        }

        public static bool SyncFromFirebaseCurrentUser()
        {
            return false;
        }

        public static UniTask<bool> ManualSignInAsync()
        {
            LogStubWarning();
            return UniTask.FromResult(false);
        }

        public static UniTask<bool> SignOutAsync()
        {
            PlayerPrefs.SetInt("__auth_explicitly_signed_out__", 1);
            PlayerPrefs.Save();
            Debug.Log("[Auth] Firebase SDK not detected. Recorded sign-out locally.");
            return UniTask.FromResult(true);
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
