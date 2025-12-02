#if BUMI_SAVE_CLOUD_FIREBASE
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using FullSerializer;
using UnityEngine;
using BumiMobile.Security;

namespace BumiMobile
{
    /// <summary>
    /// FirebaseSaveWrapper (cloud enabled): Local + Firestore sync.
    /// Layout Firestore (simplified):
    ///   players/{uid}
    /// Fields: json (string), updatedAt (Timestamp), version (int), clientTime (string ISO)
    /// Conflict: newest updatedAt wins (server vs local). Debounced uploads.
    /// </summary>
    public sealed class FirebaseSaveWrapper : BaseSaveWrapper
    {
        const string COL_PLAYERS = "players"; // root collection
        const string SUBCOL_SAVES = "saves";   // per-ISaveObject subcollection
        const string SLOT_NAME = "main";       // local file name

        readonly DefaultSaveWrapper _local = new DefaultSaveWrapper();
        static FirebaseAuth _auth;
        static FirebaseFirestore _fs;
        static bool _available;              // True when dependencies + auth + firestore ready
        static bool _pendingUpload;          // Flag for delayed push
        static GlobalSave _queuedSave;       // Last save waiting to upload
        static double _nextUploadAt;         // Stopwatch seconds when push should occur
        const double UPLOAD_DEBOUNCE = 1.5d; // seconds
        static readonly Stopwatch _watch = Stopwatch.StartNew();

        // Async init control
        static bool _initStarted;
        static Task _initTask;
        static readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);

        public override bool SupportsCloud => true;

        // Public-ish internal: ensure everything ready (dependencies + anon auth)
        async Task EnsureInitializedAsync()
        {
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

                if (_auth.CurrentUser == null)
                {
                    UnityEngine.Debug.Log("[SaveCloud] No user. Trying anonymous sign-in...");
                    try
                    {
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
            return _fs.Collection(COL_PLAYERS).Document(user.UserId);
        }

        public override GlobalSave Load(string fileName) => _local.Load(fileName);

        public override void Save(GlobalSave globalSave, string fileName)
        {
            _local.Save(globalSave, fileName);
            QueueUpload(globalSave);
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
            GlobalSave local = _local.Load(SLOT_NAME);
            try
            {
                var docRef = GetDoc(user);
                var snap = await docRef.GetSnapshotAsync();
                if (!snap.Exists)
                {
                    UnityEngine.Debug.Log("[SaveCloud] No remote player doc, keeping local.");
                    onLoaded?.Invoke(null);
                    return;
                }
                // 1) Try read per-ISaveObject split from subcollection
                var subCol = await docRef.Collection(SUBCOL_SAVES).GetSnapshotAsync();

                // 2) Also try aggregated JSON (backward-compat)
                GlobalSave aggregated = null;
                if (snap.TryGetValue("json", out string aggJsonStr) && !string.IsNullOrEmpty(aggJsonStr))
                {
                    try
                    {
                        string plain = SaveCrypto.DecryptJsonIfNeeded(aggJsonStr, user.UserId);
                        aggregated = Deserialize(plain);
                    }
                    catch (Exception decEx)
                    {
                        UnityEngine.Debug.LogWarning("[SaveCloud] Decrypt(aggregated) failed: " + decEx.Message);
                    }
                }

                if (subCol != null && subCol.Count > 0)
                {
                    // Build containers from split docs
                    var items = new List<(int hash, string json)>();
                    foreach (var doc in subCol.Documents)
                    {
                        try
                        {
                            if (!int.TryParse(doc.Id, out int hash)) continue;
                            if (!doc.TryGetValue("json", out string enc)) continue;
                            string plain = SaveCrypto.DecryptJsonIfNeeded(enc, user.UserId);
                            items.Add((hash, plain));
                        }
                        catch (Exception e)
                        {
                            UnityEngine.Debug.LogWarning("[SaveCloud] Sub save parse failed: " + e.Message);
                        }
                    }

                    if (items.Count > 0)
                    {
                        var baseGlobal = aggregated ?? new GlobalSave();
                        var rebuilt = BuildGlobalFromContainers(baseGlobal, items);
                        onLoaded?.Invoke(rebuilt);
                        return;
                    }
                }

                if (aggregated != null)
                {
                    // Fallback to old single JSON
                    onLoaded?.Invoke(aggregated);
                    return;
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
                    await EnsureInitializedAsync();
                    if (!_available || toSend == null) continue;
                    var user = _auth?.CurrentUser;
                    if (user == null)
                    {
                        UnityEngine.Debug.LogWarning("[SaveCloud] Upload skipped, no user.");
                        continue;
                    }
                    var uid = GetUserId(user);
                    try
                    {
                        var docRef = GetDoc(user);

                        // Root (meta) payload — encrypt when UID available
                        string aggregatedJson = Serialize(toSend);
                        aggregatedJson = EncryptForUser(aggregatedJson, uid, logMissingUid: true);

                        var batch = _fs.StartBatch();

                        var root = new Dictionary<string, object>
                        {
                            {"json", aggregatedJson},
                            {"updatedAt", Timestamp.GetCurrentTimestamp()},
                            {"version", 1},
                            {"clientTime", DateTime.UtcNow.ToString("o")}
                        };
                        batch.Set(docRef, root, SetOptions.MergeAll);

                        // Split per-ISaveObject (encrypt per container when possible)
                        foreach (var item in EnumerateContainers(toSend))
                        {
                            try
                            {
                                string enc = item.json ?? string.Empty;
                                enc = EncryptForUser(enc, uid, logMissingUid: false);
                                var savePayload = new Dictionary<string, object>
                                {
                                    {"json", enc},
                                    {"updatedAt", Timestamp.GetCurrentTimestamp()},
                                    {"version", 1}
                                };
                                var subRef = docRef.Collection(SUBCOL_SAVES).Document(item.hash.ToString());
                                batch.Set(subRef, savePayload, SetOptions.MergeAll);
                            }
                            catch (Exception encOne)
                            {
                                UnityEngine.Debug.LogWarning($"[SaveCloud] Per-item save failed (hash={item.hash}): " + encOne.Message);
                            }
                        }

                        await batch.CommitAsync();
                        UnityEngine.Debug.Log("[SaveCloud] Upload success (split).");
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogWarning("[SaveCloud] Upload failed: " + e.Message);
                    }
                }
            }
            finally
            {
                _uploadLoopRunning = false;
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

        static string GetUserId(FirebaseUser user)
        {
            if (user == null) return null;
            var uid = user.UserId;
            if (string.IsNullOrEmpty(uid))
            {
                UnityEngine.Debug.LogWarning("[SaveCloud] Firebase user missing UserId.");
                return null;
            }
            return uid;
        }

        static string EncryptForUser(string json, string uid, bool logMissingUid)
        {
            if (string.IsNullOrEmpty(json))
                return json ?? string.Empty;

            if (string.IsNullOrEmpty(uid))
            {
                if (logMissingUid)
                    UnityEngine.Debug.LogWarning("[SaveCloud] Skip encryption: missing UID.");
                return json;
            }

            try
            {
                return SaveCrypto.EncryptJson(json, uid);
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

        // ===== Reflection helpers to split/restore per ISaveObject ==========
        static IEnumerable<(int hash, string json)> EnumerateContainers(GlobalSave save)
        {
            if (save == null) yield break;

            var tGlobal = typeof(GlobalSave);
            var fArray = tGlobal.GetField("saveObjects", BindingFlags.Instance | BindingFlags.NonPublic);
            var fList = tGlobal.GetField("saveObjectsList", BindingFlags.Instance | BindingFlags.NonPublic);

            IEnumerable<object> containers = null;
            if (fArray?.GetValue(save) is Array arr && arr.Length > 0)
            {
                var list = new List<object>();
                foreach (var it in arr) list.Add(it);
                containers = list;
            }
            else if (fList?.GetValue(save) is System.Collections.IEnumerable listEnum)
            {
                var list = new List<object>();
                foreach (var it in listEnum) list.Add(it);
                containers = list;
            }

            if (containers == null) yield break;

            var tContainer = typeof(SavedDataContainer);
            var fHash = tContainer.GetField("hash", BindingFlags.Instance | BindingFlags.NonPublic);
            var fJson = tContainer.GetField("json", BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var c in containers)
            {
                if (c == null) continue;
                int hash = 0;
                string json = null;
                try
                {
                    if (fHash != null) hash = (int)fHash.GetValue(c);
                    if (fJson != null) json = (string)fJson.GetValue(c);
                }
                catch { /* ignore container if reflection fails */ }
                yield return (hash, json ?? string.Empty);
            }
        }

        static GlobalSave BuildGlobalFromContainers(GlobalSave baseGlobal, List<(int hash, string json)> items)
        {
            var result = baseGlobal ?? new GlobalSave();

            var tContainer = typeof(SavedDataContainer);
            var fHash = tContainer.GetField("hash", BindingFlags.Instance | BindingFlags.NonPublic);
            var fJson = tContainer.GetField("json", BindingFlags.Instance | BindingFlags.NonPublic);
            var fRestored = tContainer.GetProperty("Restored");

            var containers = new SavedDataContainer[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                var tuple = items[i];
                var inst = (SavedDataContainer)FormatterServices.GetUninitializedObject(tContainer);
                try { fHash?.SetValue(inst, tuple.hash); } catch { }
                try { fJson?.SetValue(inst, tuple.json ?? string.Empty); } catch { }
                try { fRestored?.SetValue(inst, false, null); } catch { }
                containers[i] = inst;
            }

            var tGlobal = typeof(GlobalSave);
            var fArray = tGlobal.GetField("saveObjects", BindingFlags.Instance | BindingFlags.NonPublic);
            fArray?.SetValue(result, containers);

            return result;
        }
    }
}
#endif