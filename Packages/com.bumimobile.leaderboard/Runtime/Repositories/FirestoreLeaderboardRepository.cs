#if BUMI_LEADERBOARD_HAS_FIRESTORE
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BumiMobile;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

namespace BumiMobile.Leaderboard
{
    public sealed class FirestoreLeaderboardRepository : ILeaderboardRemoteRepository
    {
        const string COL_PLAYERS = "players";
        const string COL_COUNTRY = "lb_country";

        FirebaseAuth _auth;
        FirebaseFirestore _firestore;
        bool _failed;
        bool _suppressCountryAggregate;

        public string CurrentUserId => _auth?.CurrentUser?.UserId ?? AuthService.User?.UserId;

        public async UniTask<bool> EnsureReadyAsync()
        {
            if (_failed) return false;
            if (_auth != null && _firestore != null) return true;

            try
            {
                var deps = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (deps != DependencyStatus.Available)
                {
                    Debug.LogWarning("[Leaderboard] Firebase dependencies missing: " + deps);
                    _failed = true;
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Leaderboard] Firebase dependency check failed: " + e.Message);
                _failed = true;
                return false;
            }

            _auth = FirebaseAuth.DefaultInstance;
            _firestore = FirebaseFirestore.DefaultInstance;
            if (_auth == null || _firestore == null)
            {
                Debug.LogWarning("[Leaderboard] Firebase Auth/Firestore instances unavailable.");
                _failed = true;
                return false;
            }

            return true;
        }

        public async UniTask SubmitScoreAsync(long absoluteScore, int pendingDelta, string displayName, string countryIso)
        {
            if (!await EnsureReadyAsync()) return;
            var user = _auth.CurrentUser;
            if (user == null)
                throw new InvalidOperationException("No authenticated Firebase user.");

            string uid = user.UserId;
            var playerDoc = _firestore.Collection(COL_PLAYERS).Document(uid);

            try
            {
                await _firestore.RunTransactionAsync(transaction =>
                {
                    var data = new Dictionary<string, object>
                    {
                        {"name", displayName},
                        {"score", absoluteScore},
                        {"scoreUpdatedAt", FieldValue.ServerTimestamp}
                    };
                    if (!string.IsNullOrEmpty(countryIso))
                        data["country"] = countryIso;

                    transaction.Set(playerDoc, data, SetOptions.MergeAll);
                    return Task.CompletedTask;
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Leaderboard] Submit failed: {e.Message}");
                throw;
            }

            if (!string.IsNullOrEmpty(countryIso) && !_suppressCountryAggregate && pendingDelta > 0)
            {
                await TryUpdateCountryAggregateAsync(countryIso, pendingDelta);
            }
        }

        async UniTask TryUpdateCountryAggregateAsync(string countryIso, int pendingDelta)
        {
            try
            {
                var countryDoc = _firestore.Collection(COL_COUNTRY).Document(countryIso);
                await _firestore.RunTransactionAsync(async tx =>
                {
                    var snap = await tx.GetSnapshotAsync(countryDoc);
                    long existingTotal = (snap.Exists && snap.TryGetValue("totalScore", out long exTotal)) ? exTotal : 0L;
                    long next = existingTotal + pendingDelta;
                    if (next < existingTotal) next = existingTotal;

                    tx.Set(countryDoc, new Dictionary<string, object>
                    {
                        {"country", countryIso},
                        {"totalScore", next},
                        {"updatedAt", FieldValue.ServerTimestamp}
                    }, SetOptions.MergeAll);
                });
            }
            catch (FirestoreException fex)
            {
                Debug.LogWarning($"[Leaderboard] Country aggregate error: {fex.ErrorCode} | {fex.Message}");
                if (fex.ErrorCode == FirestoreError.PermissionDenied)
                {
                    _suppressCountryAggregate = true;
                    Debug.LogWarning("[Leaderboard] Country aggregate disabled due to permissions.");
                }
            }
            catch (Exception aggEx)
            {
                Debug.LogWarning("[Leaderboard] Country aggregate failed: " + aggEx.Message);
            }
        }

        public async UniTask<List<LeaderboardEntry>> GetGlobalLeaderboardAsync(int limit)
        {
            if (!await EnsureReadyAsync()) return new List<LeaderboardEntry>();
            var query = _firestore.Collection(COL_PLAYERS)
                .OrderByDescending("score")
                .OrderByDescending("scoreUpdatedAt")
                .Limit(limit);

            var snap = await query.GetSnapshotAsync();
            return ProjectPlayerDocs(snap.Documents);
        }

        public async UniTask<List<LeaderboardEntry>> GetRegionalLeaderboardAsync(string countryIso, int limit)
        {
            if (!await EnsureReadyAsync()) return new List<LeaderboardEntry>();
            if (string.IsNullOrWhiteSpace(countryIso)) countryIso = "ZZ";
            var query = _firestore.Collection(COL_PLAYERS)
                .WhereEqualTo("country", countryIso)
                .OrderByDescending("score")
                .OrderByDescending("scoreUpdatedAt")
                .Limit(limit);

            var snap = await query.GetSnapshotAsync();
            return ProjectPlayerDocs(snap.Documents);
        }

        public async UniTask<List<LeaderboardEntry>> GetCountryLeaderboardAsync(int limit)
        {
            if (!await EnsureReadyAsync()) return new List<LeaderboardEntry>();
            var query = _firestore.Collection(COL_COUNTRY)
                .OrderByDescending("totalScore")
                .Limit(limit);

            var snap = await query.GetSnapshotAsync();
            var list = new List<LeaderboardEntry>(snap.Count);
            foreach (var doc in snap.Documents)
            {
                doc.TryGetValue("country", out string iso);
                doc.TryGetValue("totalScore", out long totalLong);
                int total = totalLong > int.MaxValue ? int.MaxValue : (int)totalLong;
                list.Add(new LeaderboardEntry
                {
                    userId = null,
                    country = iso,
                    name = iso,
                    score = total,
                    updatedAt = DateTime.UtcNow
                });
            }
            return list;
        }

        public async UniTask<LeaderboardEntry> GetPlayerEntryAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;
            if (!await EnsureReadyAsync()) return null;

            var doc = await _firestore.Collection(COL_PLAYERS).Document(userId).GetSnapshotAsync();
            return doc.Exists ? ProjectPlayerDoc(doc) : null;
        }

        static List<LeaderboardEntry> ProjectPlayerDocs(IEnumerable<DocumentSnapshot> docs)
        {
            var list = new List<LeaderboardEntry>();
            foreach (var doc in docs)
            {
                var entry = ProjectPlayerDoc(doc);
                if (entry != null)
                    list.Add(entry);
            }
            return list;
        }

        static LeaderboardEntry ProjectPlayerDoc(DocumentSnapshot doc)
        {
            if (doc == null) return null;
            doc.TryGetValue("name", out string name);
            doc.TryGetValue("country", out string country);
            doc.TryGetValue("score", out long scoreLong);
            int score = scoreLong > int.MaxValue ? int.MaxValue : (int)scoreLong;
            DateTime updated = DateTime.UtcNow;
            if (doc.TryGetValue("scoreUpdatedAt", out Timestamp ts))
            {
                updated = ts.ToDateTime();
            }

            return new LeaderboardEntry
            {
                userId = doc.Id,
                name = name,
                country = country,
                score = score,
                updatedAt = updated
            };
        }
    }
}
#endif
