#if BUMI_SAVE_CLOUD_FIREBASE
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using FullSerializer;
using UnityEngine;
using BumiMobile.Security;

namespace BumiMobile
{
    /// <summary>
    /// FirebaseSaveWrapper (cloud enabled): Local + Firestore sync.
    /// Layout Firestore (simplified):
    ///   players_saves_dev/{uid} or players_saves_prod/{uid}
    /// Fields: json (string), updatedAt (Timestamp), version (int), clientTime (string ISO)
    /// Conflict: newest updatedAt wins (server vs local). Debounced uploads.
    /// </summary>
    public sealed class FirebaseSaveWrapper : BaseSaveWrapper
    {
        const string COL_PLAYERS_SAVES_DEV = "players_saves_dev";
        const string COL_PLAYERS_SAVES_PROD = "players_saves_prod";

        readonly DefaultSaveWrapper _local = new DefaultSaveWrapper();
        static FirebaseAuth _auth;
        static FirebaseFirestore _fs;
        static bool _available;              // True when dependencies + auth + firestore ready
        static bool _pendingUpload;          // Flag for delayed push
        static GlobalSave _queuedSave;       // Last save waiting to upload
        static double _nextUploadAt;         // Stopwatch seconds when push should occur
        const double UPLOAD_DEBOUNCE = 1.5d; // seconds
        static readonly Stopwatch _watch = Stopwatch.StartNew();

        public static bool UseDevCollection { get; set; }

        // Async init control
        static bool _initStarted;
        static Task _initTask;
        static readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);

        public override bool SupportsCloud => true;

        // Public-ish internal: ensure everything ready (dependencies + auth state)
        async Task EnsureInitializedAsync()
        {
            if (AuthService.IsExplicitlySignedOut && !_available)
            {
                return;
            }

            if (_available) return;
            if (_initStarted)
            {
                if (_initTask != null) await _initTask;
                return;
            }

            await _initLock.WaitAsync();
            try
            {
                if (_available) return;
                if (_initStarted && _initTask != null)
                {
                    await _initTask;
                    return;
                }
                _initStarted = true;
                _initTask = InternalInitAsync();
            }
            finally
            {
                _initLock.Release();
            }

            await _initTask;
        }

        static async Task InternalInitAsync()
        {
            try
            {
                // Respect persistent auth opt-out
                if (UnityEngine.PlayerPrefs.GetInt("__auth_disabled__", 0) == 1)
                {
                    _available = false;
                    UnityEngine.Debug.Log("[SaveCloud] Auth disabled by user. Skipping Firebase init.");
                    return;
                }

                UnityEngine.Debug.Log("[SaveCloud] Init: checking Firebase dependencies...");
                var status = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (status != DependencyStatus.Available)
                {
                    UnityEngine.Debug.LogWarning("[SaveCloud] Dependencies not available: " + status);
                    _available = false;
                    return;
                }

                _auth = FirebaseAuth.DefaultInstance;
                if (_auth == null)
                {
                    UnityEngine.Debug.LogWarning("[SaveCloud] FirebaseAuth DefaultInstance null");
                    _available = false;
                    return;
                }

                if (AuthService.IsExplicitlySignedOut)
                {
                    try
                    {
                        _auth.SignOut();
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogWarning("[SaveCloud] Explicit sign-out cleanup failed: " + e.Message);
                    }

                    _available = false;
                    _initStarted = false;
                    _initTask = null;
                    UnityEngine.Debug.Log("[SaveCloud] Explicit sign-out active. Skipping anonymous sign-in.");
                    return;
                }

                if (_auth.CurrentUser == null)
                {
                    if (AuthService.IsExplicitlySignedOut)
                    {
                        _available = false;
                        _initStarted = false;
                        _initTask = null;
                        UnityEngine.Debug.Log("[SaveCloud] Explicit sign-out active. Anonymous sign-in skipped.");
                        return;
                    }

                    UnityEngine.Debug.Log("[SaveCloud] No user. Trying anonymous sign-in...");
                    try
                    {
                        if (AuthService.IsExplicitlySignedOut)
                        {
                            _available = false;
                            _initStarted = false;
                            _initTask = null;
                            UnityEngine.Debug.Log("[SaveCloud] Explicit sign-out active. Anonymous sign-in skipped.");
                            return;
                        }

                        await _auth.SignInAnonymouslyAsync();
                        if (_auth.CurrentUser != null)
                            UnityEngine.Debug.Log("[SaveCloud] Anonymous sign-in success (uid=" + _auth.CurrentUser.UserId + ")");
                        else
                            UnityEngine.Debug.LogWarning("[SaveCloud] Anonymous sign-in returned null user");
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogWarning("[SaveCloud] Anonymous sign-in failed: " + e.Message);
                        _available = false;
                        return;
                    }
                }

                try
                {
                    _fs = FirebaseFirestore.DefaultInstance;
                }
                catch (Exception fe)
                {
                    UnityEngine.Debug.LogWarning("[SaveCloud] Firestore init failed: " + fe.Message);
                    _available = false;
                    return;
                }

                _available = true;
                UnityEngine.Debug.Log("[SaveCloud] Init complete (uid=" + (_auth.CurrentUser?.UserId ?? "null") + ")");
            }
            catch (Exception e)
            {
                _available = false;
                UnityEngine.Debug.LogWarning("[SaveCloud] Init exception: " + e.Message);
            }
        }

        async Task<FirebaseUser> GetUserAsync()
        {
            await EnsureInitializedAsync();
            if (!_available) return null;
            return _auth?.CurrentUser;
        }

        DocumentReference GetDoc(FirebaseUser user)
        {
            string collection = UseDevCollection ? COL_PLAYERS_SAVES_DEV : COL_PLAYERS_SAVES_PROD;
            return _fs.Collection(collection).Document(GetDocKey(user));
        }

        /// <summary>
        /// Stable key for the player's cloud save document.
        /// - Google-linked users: Firebase UID (survives uninstall, portable across devices).
        /// - Anonymous users: device-unique identifier (ANDROID_ID / identifierForVendor), which
        ///   survives uninstall + reinstall, so cloud saves are not lost when the app is
        ///   removed and reinstalled (e.g. forced reinstall after a failed Play Store update).
        /// </summary>
        static string GetDocKey(FirebaseUser user)
        {
            if (user != null && !user.IsAnonymous && !string.IsNullOrEmpty(user.UserId))
            {
                return user.UserId;
            }

            return GetDeviceId();
        }

        static string GetDeviceId()
        {
            try
            {
                string deviceId = UnityEngine.SystemInfo.deviceUniqueIdentifier;
                if (!string.IsNullOrEmpty(deviceId))
                {
                    return deviceId;
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[SaveCloud] deviceUniqueIdentifier failed: " + e.Message);
            }

            return "device-fallback";
        }

        public override GlobalSave Load(string fileName) => _local.Load(fileName);

        public override void Save(GlobalSave globalSave, string fileName)
        {
            _local.Save(globalSave, fileName);
            QueueUpload(globalSave);
        }

        public override void SaveLocal(GlobalSave globalSave, string fileName)
        {
            _local.Save(globalSave, fileName);
        }

        public override void Delete(string fileName)
        {
            _local.Delete(fileName);
            _ = DeleteRemoteAsync();
        }

        // async Task DeleteRemoteAsync() { }

        public override bool UseThreads() => _local.UseThreads();

        // async Task TryPushAsync(GlobalSave global) { }

        public override void BeginCloudLoad(Action<GlobalSave> onLoaded)
        {
            _ = BeginCloudLoadAsync(onLoaded);
        }

        async Task BeginCloudLoadAsync(Action<GlobalSave> onLoaded)
        {
            await EnsureInitializedAsync();
            if (!_available)
            {
                onLoaded?.Invoke(null);
                return;
            }
            var user = await GetUserAsync();
            if (user == null)
            {
                onLoaded?.Invoke(null);
                return;
            }
            try
            {
                var docRef = GetDoc(user);
                var snap = await docRef.GetSnapshotAsync();
                if (!snap.Exists)
                {
                    UnityEngine.Debug.Log("[SaveCloud] No remote player save doc, keeping local.");
                    onLoaded?.Invoke(null);
                    return;
                }

                if (snap.TryGetValue("json", out string aggJsonStr) && !string.IsNullOrEmpty(aggJsonStr))
                {
                    try
                    {
                        string plain = SaveCrypto.DecryptJsonIfNeeded(aggJsonStr, GetDocKey(user));
                        onLoaded?.Invoke(Deserialize(plain));
                        return;
                    }
                    catch (Exception decEx)
                    {
                        UnityEngine.Debug.LogWarning("[SaveCloud] Decrypt failed: " + decEx.Message);
                    }
                }

                // No usable remote data
                onLoaded?.Invoke(null);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[SaveCloud] Cloud load failed: " + e.Message);
                onLoaded?.Invoke(null);
            }
        }

        public override void SaveCloud(GlobalSave globalSave)
        {
            QueueUpload(globalSave);
        }

        public override void SaveCloudNow(GlobalSave globalSave)
        {
            _ = SaveCloudNowAsync(globalSave);
        }

        public override async Task<bool> SaveCloudNowAsync(GlobalSave globalSave)
        {
            if (globalSave == null)
            {
                return false;
            }

            _queuedSave = globalSave;
            _pendingUpload = false;
            return await PerformUploadAsync(globalSave);
        }

        void QueueUpload(GlobalSave save)
        {
            _queuedSave = save;
            _pendingUpload = true;
            _nextUploadAt = _watch.Elapsed.TotalSeconds + UPLOAD_DEBOUNCE;
            if (!_uploadLoopRunning) _ = UploadLoop();
        }

        static bool _uploadLoopRunning;
        async Task UploadLoop()
        {
            if (_uploadLoopRunning) return;
            _uploadLoopRunning = true;
            try
            {
                while (_pendingUpload)
                {
                    if (_watch.Elapsed.TotalSeconds < _nextUploadAt)
                    {
                        await Task.Delay(200);
                        continue;
                    }
                    _pendingUpload = false;
                    var toSend = _queuedSave;
                    await PerformUploadAsync(toSend);
                }
            }
            finally
            {
                _uploadLoopRunning = false;
            }
        }

        async Task<bool> PerformUploadAsync(GlobalSave toSend)
        {
            await EnsureInitializedAsync();
            if (!_available || toSend == null)
            {
                return false;
            }

            var user = _auth?.CurrentUser;
            if (user == null)
            {
                UnityEngine.Debug.LogWarning("[SaveCloud] Upload skipped, no user.");
                return false;
            }

            var key = GetDocKey(user);
            try
            {
                var docRef = GetDoc(user);

                // One player save document only, to keep Firestore write usage low.
                string aggregatedJson = Serialize(toSend);
                aggregatedJson = EncryptForUser(aggregatedJson, key, logMissingUid: true);

                var root = new Dictionary<string, object>
                {
                    {"json", aggregatedJson},
                    {"updatedAt", Timestamp.GetCurrentTimestamp()},
                    {"version", 1},
                    {"clientTime", DateTime.UtcNow.ToString("o")}
                };
                await docRef.SetAsync(root, SetOptions.MergeAll);
                UnityEngine.Debug.Log("[SaveCloud] Upload success.");
                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[SaveCloud] Upload failed: " + e.Message);
                return false;
            }
        }

        async Task DeleteRemoteAsync()
        {
            await EnsureInitializedAsync();
            if (!_available) return;
            var user = _auth?.CurrentUser;
            if (user == null) return;
            try
            {
                await GetDoc(user).DeleteAsync();
                UnityEngine.Debug.Log("[SaveCloud] Remote delete success.");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[SaveCloud] Remote delete failed: " + e.Message);
            }
        }

        static string EncryptForUser(string json, string key, bool logMissingUid)
        {
            if (string.IsNullOrEmpty(json))
                return json ?? string.Empty;

            if (string.IsNullOrEmpty(key))
            {
                if (logMissingUid)
                    UnityEngine.Debug.LogWarning("[SaveCloud] Skip encryption: missing key.");
                return json;
            }

            try
            {
                return SaveCrypto.EncryptJson(json, key);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[SaveCloud] Encrypt failed: " + e.Message);
                return json;
            }
        }

        // ===== Serialization Helpers ========================================
        static readonly fsSerializer _serializer = new fsSerializer();

        static string Serialize(GlobalSave save)
        {
            var data = new fsData();
            _serializer.TrySerialize(save.GetType(), save, out data).AssertSuccessWithoutWarnings();
            return fsJsonPrinter.CompressedJson(data);
        }

        static GlobalSave Deserialize(string json)
        {
            try
            {
                var data = fsJsonParser.Parse(json);
                object result = null;
                _serializer.TryDeserialize(data, typeof(GlobalSave), ref result).AssertSuccessWithoutWarnings();
                return result as GlobalSave;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[SaveCloud] Deserialize failed: " + e.Message);
                return null;
            }
        }
    }
}
#endif
