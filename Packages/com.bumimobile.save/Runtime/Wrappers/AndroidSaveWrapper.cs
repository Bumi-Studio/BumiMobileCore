#if UNITY_ANDROID && BUMI_SAVE_CLOUD_GPGS
using System;
using System.Text;
using FullSerializer;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using BumiMobile.Security;

namespace BumiMobile
{
    /// <summary>
    /// Android wrapper: lokal (fallback) + Cloud (GPGS Saved Games).
    /// - Login dilakukan di sini (silent -> prompt sekali).
    /// - Slot cloud: "save"
    /// </summary>
    public sealed class AndroidSaveWrapper : BaseSaveWrapper
    {
        private readonly DefaultSaveWrapper local = new DefaultSaveWrapper();

        private const string SLOT_NAME = "save";

        public override bool SupportsCloud => true;

        // ===== Lokal I/O selalu tersedia (fallback) =====
        public override GlobalSave Load(string fileName) => local.Load(fileName);

        public override void Save(GlobalSave globalSave, string fileName)
        {
            local.Save(globalSave, fileName);
            SaveCloud(globalSave);
        }
        public override void Delete(string fileName) => local.Delete(fileName);
        public override bool UseThreads() => false;

        // ====== CLOUD (Login + Read/Write) ======
        public override void BeginCloudLoad(Action<GlobalSave> onLoaded)
        {
#if UNITY_ANDROID
            EnsureActivated();

            EnsureAuthenticated(canPromptOnce: true, authed =>
            {
                if (!authed)
                {
                    // Gagal login → fallback ke lokal
                    onLoaded?.Invoke(null);
                    return;
                }

                var client = PlayGamesPlatform.Instance.SavedGame;

                client.OpenWithAutomaticConflictResolution(
                    SLOT_NAME,
                    DataSource.ReadCacheOrNetwork,
                    ConflictResolutionStrategy.UseLongestPlaytime,
                    (openStatus, meta) =>
                    {
                        if (openStatus != SavedGameRequestStatus.Success)
                        {
                            Debug.LogError("[Save Controller] Cloud Open failed: " + openStatus);
                            onLoaded?.Invoke(null);
                            return;
                        }

                        client.ReadBinaryData(meta, (readStatus, data) =>
                        {
                            if (readStatus != SavedGameRequestStatus.Success || data == null || data.Length == 0)
                            {
                                // Kosong atau gagal → anggap fresh
                                onLoaded?.Invoke(new GlobalSave());
                                return;
                            }

                            try
                            {
                                string json = Encoding.UTF8.GetString(data);
                                if (string.IsNullOrEmpty(json))
                                {
                                    onLoaded?.Invoke(new GlobalSave());
                                    return;
                                }

                                var uid = GetCloudUserId(PlayGamesPlatform.Instance?.localUser);
                                json = SaveCrypto.DecryptJsonIfNeeded(json, uid);

                                object box = null;
                                var parsed = fsJsonParser.Parse(json);
                                var fs = new fsSerializer();
                                fs.TryDeserialize(parsed, typeof(GlobalSave), ref box)
                                  .AssertSuccessWithoutWarnings();

                                var gs = box as GlobalSave ?? new GlobalSave();
                                Debug.Log("[Save Controller] Cloud Open " + openStatus);
                                onLoaded?.Invoke(gs);
                            }
                            catch (Exception e)
                            {
                                UnityEngine.Debug.LogError("[Save Controller] Cloud Deserialize failed: " + e.Message);
                                onLoaded?.Invoke(new GlobalSave());
                            }
                        });
                    });
            });
#else
            onLoaded?.Invoke(null);
#endif
        }

        public override void SaveCloud(GlobalSave globalSave)
        {
#if UNITY_ANDROID
            if (globalSave == null) return;

            EnsureActivated();
            EnsureAuthenticated(canPromptOnce: false, authed =>
            {
                if (!authed) return;

                var client = PlayGamesPlatform.Instance.SavedGame;

                client.OpenWithAutomaticConflictResolution(
                    SLOT_NAME,
                    DataSource.ReadCacheOrNetwork,
                    ConflictResolutionStrategy.UseLongestPlaytime,
                    (openStatus, meta) =>
                    {
                        if (openStatus != SavedGameRequestStatus.Success)
                        {
                            UnityEngine.Debug.LogError("[Save Controller] Cloud Open(write) failed: " + openStatus);
                            return;
                        }

                        try
                        {
                            // Serialize + encrypt before upload
                            globalSave.Flush(true);
                            fsSerializer serializer = new fsSerializer();
                            serializer.TrySerialize(globalSave, out fsData serializedData).AssertSuccessWithoutWarnings();
                            string json = fsJsonPrinter.CompressedJson(serializedData);

                            var uid = GetCloudUserId(PlayGamesPlatform.Instance?.localUser);
                            if (!string.IsNullOrEmpty(uid))
                            {
                                json = SaveCrypto.EncryptJson(json, uid);
                            }

                            byte[] bytes = Encoding.UTF8.GetBytes(json);

                            // Metadata update
                            var builder = new SavedGameMetadataUpdate.Builder()
                                .WithUpdatedDescription("Updated: " + DateTime.UtcNow.ToString("u"));

                            // Optional: isi played time (jika ada GameTime dalam detik)
                            try
                            {
                                var seconds = Math.Max(0, (int)globalSave.GameTime);
                                builder = builder.WithUpdatedPlayedTime(TimeSpan.FromSeconds(seconds));
                            }
                            catch { /* ignore */ }

                            var update = builder.Build();


                            client.CommitUpdate(
                                meta,
                                update,
                                bytes,
                                (commitStatus, committedMeta) =>
                                {
                                    Debug.Log("[Save Controller] Cloud Commit: " + commitStatus);
                                });
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("[Save Controller] Cloud Serialize failed: " + e.Message);
                        }
                    });
            });
#endif
        }

#if UNITY_ANDROID 
        private static void EnsureActivated()
        {
            PlayGamesPlatform.Activate();
        }

        /// <summary>
        /// Coba silent sign-in dulu. Kalau gagal dan canPromptOnce=true, baru prompt sekali.
        /// </summary>
        private static void EnsureAuthenticated(bool canPromptOnce, Action<bool> onResult)
        {
            var pgp = PlayGamesPlatform.Instance;
            bool IsAuthed()
            {
                try { return pgp?.localUser is { authenticated: true }; }
                catch { return false; }
            }

            if (IsAuthed())
            {
                onResult?.Invoke(true);
                return;
            }

            // Silent first
            pgp.Authenticate(status =>
            {
                if (status == SignInStatus.Success || IsAuthed())
                {
                    onResult?.Invoke(true);
                }
                else if (canPromptOnce)
                {
                    pgp.Authenticate(promptStatus =>
                    {
                        onResult?.Invoke(promptStatus == SignInStatus.Success || IsAuthed());
                    });
                }
                else
                {
                    onResult?.Invoke(false);
                }
            });
        }

        private static string GetCloudUserId(ILocalUser user)
        {
            if (user == null) return null;

            try
            {
                if (!string.IsNullOrEmpty(user.id))
                    return user.id;
            }
            catch { }

            try
            {
                if (!string.IsNullOrEmpty(user.userName))
                    return user.userName;
            }
            catch { }

            return null;
        }
#endif
    }
#endif
