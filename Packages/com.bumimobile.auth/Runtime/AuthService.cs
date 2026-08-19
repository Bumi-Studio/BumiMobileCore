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
        private const string PREF_GOOGLE_PICKER_SUPPRESSED = "__google_picker_suppressed__";

        public const string DEFAULT_GOOGLE_WEB_CLIENT_ID =
            "1098096377753-1u3n2oias158bhpg2jpficvh7dt42b5h.apps.googleusercontent.com";

        public static string PlayerId { get; private set; } = string.Empty;
        public static FirebaseUser User { get; private set; }
        public static bool IsFirebaseAnonymous => User != null && User.IsAnonymous;
        public static bool IsAuthenticated => User != null;
        public static bool IsSignedIn => User != null && !User.IsAnonymous;
        public static string LastAuthFailureReason { get; private set; } = string.Empty;
        public static string WebClientId { get; private set; }

        public static bool IsExplicitlySignedOut =>
            PlayerPrefs.GetInt(PREF_USER_EXPLICITLY_SIGNED_OUT, 0) == 1;

        public static bool IsGooglePickerSuppressed =>
            PlayerPrefs.GetInt(PREF_GOOGLE_PICKER_SUPPRESSED, 0) == 1;

        private static bool _busy;
        private static bool _startupPickerAttempted;
        private static FirebaseAuth _firebaseAuthInstance;
        private static bool _firebaseAuthStateListenerAttached;
        private static bool _lastGoogleInteractionCancelled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStartupPickerState()
        {
            _startupPickerAttempted = false;
        }

        public static event Action<bool> OnPlatformAuthFinished;
        public static event Action<FirebaseUser> OnFirebaseAuthChanged;

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

        public static void Initialize(string webClientId)
        {
            if (string.Equals(WebClientId, webClientId, StringComparison.Ordinal)) return;
            WebClientId = webClientId;
            _busy = false;
        }

        public static bool SyncFromFirebaseCurrentUser()
        {
            try
            {
                var auth = FirebaseAuth.DefaultInstance;
                if (auth == null) return false;

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

                bool changed = User == null ||
                    !string.Equals(User.UserId, currentUser.UserId, StringComparison.Ordinal) ||
                    User.IsAnonymous != currentUser.IsAnonymous;

                User = currentUser;
                if (changed) OnFirebaseAuthChanged?.Invoke(User);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Auth] Failed to sync Firebase session: " + e.Message);
                return false;
            }
        }

        private static void EnsureFirebaseAuthStateListener(FirebaseAuth auth)
        {
            if (auth == null) return;
            if (_firebaseAuthStateListenerAttached && ReferenceEquals(_firebaseAuthInstance, auth)) return;

            if (_firebaseAuthStateListenerAttached && _firebaseAuthInstance != null)
                _firebaseAuthInstance.StateChanged -= OnFirebaseAuthStateChanged;

            _firebaseAuthInstance = auth;
            _firebaseAuthInstance.StateChanged += OnFirebaseAuthStateChanged;
            _firebaseAuthStateListenerAttached = true;
        }

        private static void OnFirebaseAuthStateChanged(object sender, EventArgs e)
        {
            SyncFromFirebaseCurrentUser();
        }

        public static async UniTask<bool> SignInAsync(CancellationToken cancellationToken = default)
        {
            return await SignInAtStartupAsync(false, cancellationToken);
        }

        /// <summary>
        /// Restores an existing account and limits the Google account flow to
        /// usable/previously authorized accounts. The full unfiltered account
        /// picker is never used during automatic startup authentication.
        /// </summary>
        public static async UniTask<bool> SignInAtStartupAsync(
            bool showPickerWhenNoAccount,
            CancellationToken cancellationToken = default)
        {
            if (_busy)
            {
                Debug.LogWarning("[Auth] Sign-in already running");
                return IsSignedIn;
            }

            _busy = true;
            try
            {
                return await SignInAtStartupInternalAsync(showPickerWhenNoAccount, cancellationToken);
            }
            finally
            {
                _busy = false;
            }
        }

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
            finally
            {
                _busy = false;
            }
        }

        public static async UniTask<bool> SignOutAsync()
        {
            try
            {
                await SignOutAllAsync();
                PlayerPrefs.SetInt(PREF_USER_EXPLICITLY_SIGNED_OUT, 1);
                PlayerPrefs.SetInt(PREF_GOOGLE_PICKER_SUPPRESSED, 1);
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

        private static async UniTask<bool> SignInAtStartupInternalAsync(
            bool showPickerWhenNoAccount,
            CancellationToken cancellationToken)
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
            EnsureFirebaseAuthStateListener(auth);

            if (IsExplicitlySignedOut)
            {
                Debug.Log("[Auth] Explicit sign-out active. Skipping automatic authentication.");
                SignOutFirebaseOnly(auth);
                return false;
            }

            // Firebase persistence is the authoritative fast path. A restored
            // non-anonymous user must never cause another Google picker.
            if (SyncFromFirebaseCurrentUser() && !User.IsAnonymous)
            {
                PersistPlayerId(User.UserId);
                return true;
            }

            if (IsGooglePickerSuppressed)
            {
                Debug.Log("[Auth] Google picker suppressed after cancellation.");
                if (SyncFromFirebaseCurrentUser() && User.IsAnonymous) return false;
                return await CreateAnonymousAsync(cancellationToken);
            }

#if BUMI_AUTH_HAS_GOOGLE_SIGNIN
            if (showPickerWhenNoAccount &&
                !string.IsNullOrEmpty(WebClientId) &&
                !_startupPickerAttempted)
            {
                // Use the filtered Google Credential Manager route once. Do
                // not fall back to SignIn(), which displays every device
                // account after the usable-account selector.
                _startupPickerAttempted = true;
                var googleUser = await SignInUsableGoogleAccountAsync(cancellationToken);
                if (googleUser != null && await TryAuthFirebaseWithGoogleAsync(googleUser, cancellationToken))
                {
                    PersistPlayerId(googleUser.UserId);
                    return true;
                }

                if (_lastGoogleInteractionCancelled)
                {
                    SuppressAutomaticPicker();
                    Debug.Log("[Auth] Google usable-account picker cancelled; suppressing future automatic prompts.");
                }
            }
#endif

            if (SyncFromFirebaseCurrentUser()) return !User.IsAnonymous;
            return await CreateAnonymousAsync(cancellationToken);
        }

#if BUMI_AUTH_HAS_GOOGLE_SIGNIN
        private static async UniTask<bool> SignInManualAsync()
        {
            if (string.IsNullOrEmpty(WebClientId))
            {
                LastAuthFailureReason = "WebClientId not configured";
                Debug.LogError("[Auth] WebClientId not configured.");
                return false;
            }

            var dep = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dep != DependencyStatus.Available)
            {
                Debug.LogWarning($"[Auth] Firebase deps: {dep}");
                return false;
            }

            EnsureFirebaseAuthStateListener(FirebaseAuth.DefaultInstance);
            // Settings sign-in uses the same filtered account route as init.
            // This prevents the Settings button from opening the all-accounts
            // picker after init only showed usable accounts.
            var googleUser = await SignInUsableGoogleAccountAsync();
            if (googleUser == null)
            {
                if (_lastGoogleInteractionCancelled) SuppressAutomaticPicker();
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
        private static UniTask<bool> SignInManualAsync()
        {
            Debug.Log("[Auth] Google Sign-In SDK not available.");
            return UniTask.FromResult(false);
        }
#endif

        private static void SuppressAutomaticPicker()
        {
            PlayerPrefs.SetInt(PREF_GOOGLE_PICKER_SUPPRESSED, 1);
            PlayerPrefs.Save();
        }

        private static void SignOutFirebaseOnly(FirebaseAuth auth)
        {
            try { auth?.SignOut(); }
            catch (Exception e) { Debug.LogWarning("[Auth] Automatic sign-out cleanup failed: " + e.Message); }

            User = null;
            OnFirebaseAuthChanged?.Invoke(null);
        }

        private static async UniTask SignOutAllAsync()
        {
#if BUMI_AUTH_HAS_GOOGLE_SIGNIN
            try
            {
                if (EnsureGoogleConfigured()) GoogleSignIn.DefaultInstance.SignOut();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Auth] Google Sign-Out error: " + e.Message);
            }
#endif

            var dep = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dep == DependencyStatus.Available) FirebaseAuth.DefaultInstance?.SignOut();

            PlayerId = string.Empty;
            User = null;
            if (PlayerPrefs.HasKey(PREF_PLAYER_ID)) PlayerPrefs.DeleteKey(PREF_PLAYER_ID);
            PlayerPrefs.Save();
            OnFirebaseAuthChanged?.Invoke(null);
        }

#if BUMI_AUTH_HAS_GOOGLE_SIGNIN
        private static GoogleSignInConfiguration _cachedGoogleConfig;
        private static string _cachedWebClientIdForConfig;

        private static bool EnsureGoogleConfigured()
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
                    UseGameSignIn = false,
                    HidePopups = true
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

        private static async UniTask<GoogleSignInUser> SignInUsableGoogleAccountAsync(
            CancellationToken cancellationToken = default)
        {
            _lastGoogleInteractionCancelled = false;
            if (!EnsureGoogleConfigured())
            {
                OnPlatformAuthFinished?.Invoke(false);
                return null;
            }

            try
            {
                Debug.Log("[Auth] Requesting usable Google accounts only.");
                var googleUser = await GoogleSignIn.DefaultInstance.SignInSilently()
                    .AsUniTask()
                    .AttachExternalCancellation(cancellationToken);

                if (googleUser == null)
                {
                    _lastGoogleInteractionCancelled = false;
                    LastAuthFailureReason = "[Auth] No usable Google account was selected.";
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
                bool cancelled = e is OperationCanceledException ||
                    e.Message?.Contains("cancel", StringComparison.OrdinalIgnoreCase) == true;
                _lastGoogleInteractionCancelled = cancelled;
                LastAuthFailureReason = cancelled
                    ? "User cancelled Google Sign-In"
                    : "[Auth] Google Sign-In error: " + e.Message;
                Debug.LogWarning(LastAuthFailureReason);
                OnPlatformAuthFinished?.Invoke(false);
                return null;
            }
        }

        private static async UniTask<bool> TryAuthFirebaseWithGoogleAsync(
            GoogleSignInUser googleUser,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (googleUser == null || string.IsNullOrEmpty(googleUser.IdToken)) return false;
                var auth = FirebaseAuth.DefaultInstance;
                var cred = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);
                if (cred == null || auth == null) return false;

                if (auth.CurrentUser == null)
                {
                    User = await auth.SignInWithCredentialAsync(cred).AsUniTask().AttachExternalCancellation(cancellationToken);
                }
                else if (auth.CurrentUser.IsAnonymous)
                {
                    try
                    {
                        await auth.CurrentUser.LinkWithCredentialAsync(cred).AsUniTask().AttachExternalCancellation(cancellationToken);
                        await auth.CurrentUser.ReloadAsync().AsUniTask().AttachExternalCancellation(cancellationToken);
                        User = auth.CurrentUser;
                    }
                    catch (Exception e) when (IsCredentialAlreadyInUseError(e))
                    {
                        User = await auth.SignInWithCredentialAsync(cred).AsUniTask().AttachExternalCancellation(cancellationToken);
                    }
                }
                else
                {
                    User = await auth.SignInWithCredentialAsync(cred).AsUniTask().AttachExternalCancellation(cancellationToken);
                }

                if (User != null) OnFirebaseAuthChanged?.Invoke(User);
                return User != null;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                LastAuthFailureReason = e.Message;
                Debug.LogWarning("[Auth] Firebase Google step failed: " + e.Message);
                return false;
            }
        }
#endif

        private static async UniTask<bool> CreateAnonymousAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var dep = await FirebaseApp.CheckAndFixDependenciesAsync()
                    .AsUniTask()
                    .AttachExternalCancellation(cancellationToken);
                if (dep != DependencyStatus.Available) return false;

                var result = await FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync()
                    .AsUniTask()
                    .AttachExternalCancellation(cancellationToken);
                User = result?.User;
                if (User != null)
                {
                    Debug.Log("[Auth] Anonymous OK: " + User.UserId);
                    OnFirebaseAuthChanged?.Invoke(User);
                    OnPlatformAuthFinished?.Invoke(false);
                    return true;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Auth] Anonymous sign-in failed: " + e.Message);
            }

            OnPlatformAuthFinished?.Invoke(false);
            return false;
        }

        private static void PersistPlayerId(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return;
            PlayerId = playerId;
            PlayerPrefs.SetString(PREF_PLAYER_ID, playerId);
            PlayerPrefs.DeleteKey(PREF_USER_EXPLICITLY_SIGNED_OUT);
            PlayerPrefs.DeleteKey(PREF_GOOGLE_PICKER_SUPPRESSED);
            PlayerPrefs.Save();
        }

        private static bool IsCredentialAlreadyInUseError(Exception e)
        {
            if (e == null) return false;
            var msg = (e.Message ?? string.Empty).ToLowerInvariant();
            return msg.Contains("already associated") || msg.Contains("already linked") ||
                msg.Contains("already in use") || msg.Contains("already exists") ||
                msg.Contains("different sign-in credentials") || msg.Contains("another account") ||
                IsCredentialAlreadyInUseError(e.InnerException);
        }
    }
}
#else
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
        public static bool IsGooglePickerSuppressed => PlayerPrefs.GetInt("__google_picker_suppressed__", 0) == 1;
        public static string LastAuthFailureReason { get; private set; } = "Firebase SDK missing.";
        public static string WebClientId { get; private set; }

        public static void Initialize(string webClientId) => WebClientId = webClientId;
        public static bool SyncFromFirebaseCurrentUser() => false;
        public static event System.Action<bool> OnPlatformAuthFinished;
        public static event System.Action<FirebaseUser> OnFirebaseAuthChanged;

        public static UniTask<bool> SignInAsync(CancellationToken cancellationToken = default) => UniTask.FromResult(false);
        public static UniTask<bool> SignInAtStartupAsync(bool showPickerWhenNoAccount, CancellationToken cancellationToken = default) => UniTask.FromResult(false);
        public static UniTask<bool> ManualSignInAsync() => UniTask.FromResult(false);

        public static UniTask<bool> SignOutAsync()
        {
            PlayerPrefs.SetInt("__auth_explicitly_signed_out__", 1);
            PlayerPrefs.SetInt("__google_picker_suppressed__", 1);
            PlayerPrefs.Save();
            return UniTask.FromResult(true);
        }

        public static string GetUserLabel() => string.Empty;
    }
}
#endif
