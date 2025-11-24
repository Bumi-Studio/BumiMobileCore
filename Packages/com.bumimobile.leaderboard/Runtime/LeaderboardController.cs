using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BumiMobile;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BumiMobile.Leaderboard
{
    public static class LeaderboardController
    {
        public static event Action<LeaderboardType> OnLeaderboardUpdated;

        public const int DEFAULT_LIMIT = 10;
        public static bool IsInitialized { get; private set; }

        static ILeaderboardRemoteRepository _repository = CreateDefaultRepository();
        static string CurrentUserId => _repository?.CurrentUserId ?? AuthService.User?.UserId;

        class CacheBucket
        {
            public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
            public DateTime lastFetchUtc;
            public bool fetching;
        }

        static readonly Dictionary<LeaderboardType, CacheBucket> _cache = new Dictionary<LeaderboardType, CacheBucket>
        {
            { LeaderboardType.Global,  new CacheBucket() },
            { LeaderboardType.Regional,new CacheBucket() },
            { LeaderboardType.Country, new CacheBucket() },
        };

        static readonly List<LoadingTask> _loadingTasks = new List<LoadingTask>();
        static Task _warmupTask;

        const int CACHE_TTL_SECONDS = 30;

        public static void Init()
        {
            if (IsInitialized) return;
            IsInitialized = true;
            InitializeAsync().Forget();
        }

        public static void SetRepository(ILeaderboardRemoteRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        static ILeaderboardRemoteRepository CreateDefaultRepository()
        {
#if BUMI_LEADERBOARD_HAS_FIRESTORE
            return new FirestoreLeaderboardRepository();
#else
            return new NoopLeaderboardRepository();
#endif
        }

        static async UniTaskVoid InitializeAsync()
        {
            bool ready = await EnsureRepositoryReadyAsync();
            if (!ready)
            {
                Debug.LogWarning("[Leaderboard] Repository initialization failed.");
                return;
            }

            Debug.Log("[Leaderboard] Initialized leaderboard repository (authUser=" + (CurrentUserId ?? "null") + ")");
        }

        public static void RegisterLoadingTask(LoadingTask task)
        {
            if (task != null && !_loadingTasks.Contains(task))
                _loadingTasks.Add(task);
        }

        public static void ClearRegisteredLoadingTasks() => _loadingTasks.Clear();

        public static void InvalidateAllCache()
        {
            foreach (var kv in _cache)
                kv.Value.lastFetchUtc = DateTime.MinValue;
        }

        public static async UniTask EnsureWarmup(int topLimit = 10)
        {
            if (_warmupTask == null || _warmupTask.IsFaulted || _warmupTask.IsCanceled)
            {
                _warmupTask = UniTask.WhenAll(
                    GetCachedAsync(LeaderboardType.Global, topLimit, false),
                    GetCachedAsync(LeaderboardType.Regional, topLimit, false)
                ).AsTask();
            }
            await _warmupTask;
        }

        public static UniTask SubmitScore(int delta, string username = null, string country = null)
        {
            return SubmitScoreInternal(delta, username, country);
        }

        public static UniTask SubmitPendingScore(string username = null, string country = null)
        {
            return SubmitScoreInternal(null, username, country);
        }

        static async UniTask SubmitScoreInternal(int? delta, string username, string country)
        {
            if (!await EnsureRepositoryReadyAsync()) return;

            LeaderboardSaveService.EnsureInitialized();
            if (delta.HasValue && delta.Value != 0)
            {
                LeaderboardSaveService.AddScore(delta.Value);
            }

            if (!LeaderboardSaveService.HasPending)
            {
                Debug.Log("[Leaderboard] Submit skipped: no pending score delta.");
                return;
            }

            string uid = CurrentUserId;
            if (string.IsNullOrEmpty(uid))
            {
                Debug.LogWarning("[Leaderboard] Submit aborted: no authenticated user.");
                return;
            }

            string fallbackName = AuthService.GetUserLabel();
            if (string.IsNullOrWhiteSpace(fallbackName))
                fallbackName = "Player";

            string name = string.IsNullOrWhiteSpace(username) ? fallbackName : username.Trim();

            string rawCountry = string.IsNullOrWhiteSpace(country) ? CountryService.CountryISO : country;
            string ctry = (!string.IsNullOrWhiteSpace(rawCountry) && rawCountry.Length == 2 && !rawCountry.Equals("ZZ", StringComparison.OrdinalIgnoreCase))
                ? rawCountry.ToUpperInvariant()
                : null;

            long absoluteScore = LeaderboardSaveService.TotalScore;
            int pendingDelta = LeaderboardSaveService.PendingScore;

            Debug.Log($"[Leaderboard] Submit start uid={uid} total={absoluteScore} delta={pendingDelta} country={(ctry ?? "<none>")} name={name}");

            try
            {
                await _repository.SubmitScoreAsync(absoluteScore, pendingDelta, name, ctry);

                LeaderboardSaveService.MarkSubmissionSuccess(name, ctry);

                InvalidateAllCache();
                OnLeaderboardUpdated?.Invoke(LeaderboardType.Global);
                OnLeaderboardUpdated?.Invoke(LeaderboardType.Regional);
                OnLeaderboardUpdated?.Invoke(LeaderboardType.Country);
                Debug.Log($"[Leaderboard] Submit finished uid={uid}");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Leaderboard] Submit failed: " + e.Message);
            }
        }

        public static UniTask<List<LeaderboardEntry>> GetGlobalLeaderboard(int limit = DEFAULT_LIMIT) => QueryPlayersGlobal(limit);
        public static UniTask<List<LeaderboardEntry>> GetRegionLeaderboard(int limit = DEFAULT_LIMIT)
            => QueryPlayersByCountry(CountryService.CountryISO, limit);
        public static UniTask<List<LeaderboardEntry>> GetCountryLeaderboard(int limit = DEFAULT_LIMIT)
            => QueryCountryAggregates(limit);

        static async UniTask<List<LeaderboardEntry>> GetCachedAsyncCore(LeaderboardType type, int limit, bool forceRefresh)
        {
            if (!await EnsureRepositoryReadyAsync()) return new List<LeaderboardEntry>();
            if (!_cache.TryGetValue(type, out var bucket)) return new List<LeaderboardEntry>();

            if (!forceRefresh && bucket.entries.Count > 0 && (DateTime.UtcNow - bucket.lastFetchUtc).TotalSeconds < CACHE_TTL_SECONDS)
            {
                return Trim(bucket.entries, limit);
            }

            if (bucket.fetching)
            {
                for (int i = 0; i < 100; i++)
                {
                    await UniTask.Delay(20);
                    if (!bucket.fetching) break;
                }
                return Trim(bucket.entries, limit);
            }

            bucket.fetching = true;
            try
            {
                List<LeaderboardEntry> fresh = type switch
                {
                    LeaderboardType.Global => await GetGlobalLeaderboard(limit),
                    LeaderboardType.Regional => await GetRegionLeaderboard(limit),
                    LeaderboardType.Country => await GetCountryLeaderboard(limit),
                    _ => new List<LeaderboardEntry>()
                };
                bucket.entries = fresh ?? new List<LeaderboardEntry>();
                bucket.lastFetchUtc = DateTime.UtcNow;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Leaderboard] Fetch failed for " + type + ": " + e.Message);
            }
            finally
            {
                bucket.fetching = false;
            }

            return Trim(bucket.entries, limit);
        }

        public static UniTask<List<LeaderboardEntry>> GetCachedAsync(LeaderboardType type, int limit = DEFAULT_LIMIT, bool forceRefresh = false)
            => GetCachedAsyncCore(type, limit, forceRefresh);

        static async UniTask<bool> EnsureRepositoryReadyAsync()
        {
            if (!IsInitialized) Init();
            if (_repository == null)
                _repository = CreateDefaultRepository();
            return await _repository.EnsureReadyAsync();
        }

        static List<LeaderboardEntry> Trim(List<LeaderboardEntry> source, int limit)
        {
            if (source == null) return new List<LeaderboardEntry>();
            if (source.Count <= limit) return source;
            return source.Take(limit).ToList();
        }

        static async UniTask<List<LeaderboardEntry>> QueryPlayersGlobal(int limit)
        {
            var list = await _repository.GetGlobalLeaderboardAsync(limit);
            return list ?? new List<LeaderboardEntry>();
        }

        static async UniTask<List<LeaderboardEntry>> QueryPlayersByCountry(string countryIso, int limit)
        {
            if (string.IsNullOrWhiteSpace(countryIso)) countryIso = "ZZ";
            var countryUpper = countryIso.ToUpperInvariant();
            var list = await _repository.GetRegionalLeaderboardAsync(countryUpper, limit);
            return list ?? new List<LeaderboardEntry>();
        }

        static async UniTask<List<LeaderboardEntry>> QueryCountryAggregates(int limit)
        {
            var docs = await _repository.GetCountryLeaderboardAsync(limit);
            var list = new List<LeaderboardEntry>();
            if (docs != null)
            {
                foreach (var entry in docs)
                {
                    if (entry == null) continue;
                    entry.name = ResolveCountryDisplayName(entry.country);
                    list.Add(entry);
                }
            }
            return list;
        }

        static string ResolveCountryDisplayName(string iso)
        {
            if (string.IsNullOrWhiteSpace(iso)) return "Unknown";
            iso = iso.ToUpperInvariant();
            var db = CountryService.FlagDatabase;
            if (db != null)
            {
                var n = db.GetDisplayName(iso);
                if (!string.IsNullOrWhiteSpace(n)) return n;
            }
            if (!string.IsNullOrWhiteSpace(CountryService.CountryISO) && CountryService.CountryISO.Equals(iso, StringComparison.OrdinalIgnoreCase))
            {
                return string.IsNullOrWhiteSpace(CountryService.CountryName) ? iso : CountryService.CountryName;
            }
            return iso;
        }

        public static async UniTask<LeaderboardEntry> GetSelfEntryAsync()
        {
            if (!await EnsureRepositoryReadyAsync())
                return null;

            var uid = CurrentUserId;
            if (string.IsNullOrEmpty(uid))
                return null;

            return await _repository.GetPlayerEntryAsync(uid);
        }

        public static string GetCurrentUserId() => CurrentUserId ?? string.Empty;
    }
}
